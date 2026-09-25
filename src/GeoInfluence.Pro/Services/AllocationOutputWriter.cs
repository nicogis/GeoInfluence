using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using DdlFieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using GeoInfluence.Core.Allocation;
using ArcGIS.Core.CIM;

namespace GeoInfluence.Pro.Services;

internal static class AllocationOutputWriter
{
    private static readonly (int R, int G, int B)[] Palette =
    [
        (31, 119, 180),
        (255, 127, 14),
        (44, 160, 44),
        (214, 39, 40),
        (148, 103, 189),
        (140, 86, 75),
        (227, 119, 194),
        (127, 127, 127),
        (188, 189, 34),
        (23, 190, 207)
    ];
    internal sealed record OutputResult(
        string FeatureClassName,
        string GeodatabasePath,
        int FeatureCount);

    public static OutputResult WriteToDefaultGeodatabase(
        AllocationGrid grid,
        SpatialReference workingSpatialReference)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(workingSpatialReference);

        var map = MapView.Active?.Map
            ?? throw new InvalidOperationException("No active map is available.");

        var geodatabasePath = Project.Current.DefaultGeodatabasePath;
        if (string.IsNullOrWhiteSpace(geodatabasePath))
            throw new InvalidOperationException("The project has no default geodatabase.");

        var featureClassName = $"GeoInfluence_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}";

        using var geodatabase = new Geodatabase(
            new FileGeodatabaseConnectionPath(
                new Uri(geodatabasePath)));

        var fields = new List<DdlFieldDescription>
        {
            DdlFieldDescription.CreateStringField("SiteId", 128),
            new DdlFieldDescription("Score", FieldType.Double),
            DdlFieldDescription.CreateIntegerField("GridRow"),
            DdlFieldDescription.CreateIntegerField("GridCol")
        };

        var shapeDescription = new ShapeDescription(
            GeometryType.Polygon,
            workingSpatialReference);

        var featureClassDescription = new FeatureClassDescription(
            featureClassName,
            fields,
            shapeDescription);

        var schemaBuilder = new SchemaBuilder(geodatabase);
        schemaBuilder.Create(featureClassDescription);

        if (!schemaBuilder.Build())
        {
            var errors = string.Join(
                Environment.NewLine,
                schemaBuilder.ErrorMessages);

            throw new InvalidOperationException(
                $"Unable to create output feature class '{featureClassName}'. {errors}");
        }

        using var featureClass =
            geodatabase.OpenDataset<FeatureClass>(featureClassName);

        using var definition = featureClass.GetDefinition();
        var shapeField = definition.GetShapeField();

        geodatabase.ApplyEdits(() =>
        {
            foreach (var cell in grid.Cells)
            {
                var xMin = grid.XMin + (cell.Column * grid.CellWidth);
                var yMin = grid.YMin + (cell.Row * grid.CellHeight);
                var xMax = xMin + grid.CellWidth;
                var yMax = yMin + grid.CellHeight;

                var envelope = EnvelopeBuilderEx.CreateEnvelope(
                    xMin,
                    yMin,
                    xMax,
                    yMax,
                    workingSpatialReference);

                var polygon = PolygonBuilderEx.CreatePolygon(envelope);

                using var rowBuffer = featureClass.CreateRowBuffer();
                rowBuffer[shapeField] = polygon;
                rowBuffer["SiteId"] = cell.SiteId;
                rowBuffer["Score"] = cell.Score;
                rowBuffer["GridRow"] = cell.Row;
                rowBuffer["GridCol"] = cell.Column;

                using var row = featureClass.CreateRow(rowBuffer);
            }
        });

        var outputLayer = LayerFactory.Instance.CreateLayer<FeatureLayer>(
            new FeatureLayerCreationParams(featureClass)
            {
                Name = featureClassName
            },
            map);

        outputLayer.SetRenderer(
            CreateSiteIdRenderer(grid));

        return new OutputResult(
            featureClassName,
            geodatabasePath,
            grid.Cells.Count);
    }

    internal static CIMRenderer CreateSiteIdRenderer(AllocationGrid grid)
    {
        var classes = grid.Cells
            .GroupBy(cell => new { cell.SiteIndex, cell.SiteId })
            .OrderBy(group => group.Key.SiteIndex)
            .Select(group =>
            {
                var color = Palette[group.Key.SiteIndex % Palette.Length];

                var fill = ColorFactory.Instance.CreateRGBColor(
                    color.R,
                    color.G,
                    color.B,
                    75);

                var outline = SymbolFactory.Instance.ConstructStroke(
                    ColorFactory.Instance.CreateRGBColor(255, 255, 255, 90),
                    0.5,
                    SimpleLineStyle.Solid);

                var symbol = SymbolFactory.Instance
                    .ConstructPolygonSymbol(
                        fill,
                        SimpleFillStyle.Solid,
                        outline)
                    .MakeSymbolReference();

                return new CIMUniqueValueClass
                {
                    Values =
                    [
                        new CIMUniqueValue
                        {
                            FieldValues = [group.Key.SiteId]
                        }
                    ],
                    Label = group.Key.SiteId,
                    Visible = true,
                    Editable = true,
                    Symbol = symbol
                };
            })
            .ToArray();

        return new CIMUniqueValueRenderer
        {
            Fields = ["SiteId"],
            Groups =
            [
                new CIMUniqueValueGroup
                {
                    Heading = "Influence site",
                    Classes = classes
                }
            ],
            UseDefaultSymbol = false
        };
    }
}
