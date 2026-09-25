using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using DdlFieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using GeoInfluence.Core.Allocation;

namespace GeoInfluence.Pro.Services;

internal static class InfluenceRegionOutputWriter
{
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

        var featureClassName =
            $"GeoInfluence_Regions_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}";

        using var geodatabase = new Geodatabase(
            new FileGeodatabaseConnectionPath(
                new Uri(geodatabasePath)));

        var fields = new List<DdlFieldDescription>
        {
            DdlFieldDescription.CreateStringField("SiteId", 128),
            DdlFieldDescription.CreateIntegerField("CellCount"),
            new DdlFieldDescription("MinScore", FieldType.Double),
            new DdlFieldDescription("MaxScore", FieldType.Double),
            new DdlFieldDescription("AvgScore", FieldType.Double)
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

        var groups = grid.Cells
            .GroupBy(cell => new { cell.SiteIndex, cell.SiteId })
            .OrderBy(group => group.Key.SiteIndex)
            .ToList();

        geodatabase.ApplyEdits(() =>
        {
            foreach (var group in groups)
            {
                var polygons = group
                    .Select(cell => CreateCellPolygon(
                        grid,
                        cell,
                        workingSpatialReference))
                    .Cast<Geometry>()
                    .ToList();

                var region = GeometryEngine.Instance.Union(polygons) as Polygon
                    ?? throw new InvalidOperationException(
                        $"Unable to dissolve cells for site '{group.Key.SiteId}'.");

                if (!GeometryEngine.Instance.IsSimpleAsFeature(region))
                {
                    region = GeometryEngine.Instance.SimplifyAsFeature(region) as Polygon
                        ?? throw new InvalidOperationException(
                            $"Unable to simplify dissolved region for site '{group.Key.SiteId}'.");
                }

                if (!double.IsFinite(region.Area) || Math.Abs(region.Area) <= 0)
                {
                    throw new InvalidOperationException(
                        $"Dissolved region for site '{group.Key.SiteId}' has zero area.");
                }

                using var rowBuffer = featureClass.CreateRowBuffer();
                rowBuffer[shapeField] = region;
                rowBuffer["SiteId"] = group.Key.SiteId;
                rowBuffer["CellCount"] = group.Count();
                rowBuffer["MinScore"] = group.Min(cell => cell.Score);
                rowBuffer["MaxScore"] = group.Max(cell => cell.Score);
                rowBuffer["AvgScore"] = group.Average(cell => cell.Score);

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
            AllocationOutputWriter.CreateSiteIdRenderer(grid));

        return new OutputResult(
            featureClassName,
            geodatabasePath,
            groups.Count);
    }

    private static Polygon CreateCellPolygon(
        AllocationGrid grid,
        AllocationCell cell,
        SpatialReference spatialReference)
    {
        var xMin = grid.XMin + (cell.Column * grid.CellWidth);
        var yMin = grid.YMin + (cell.Row * grid.CellHeight);
        var xMax = xMin + grid.CellWidth;
        var yMax = yMin + grid.CellHeight;

        var coordinates = new[]
        {
            new Coordinate2D(xMin, yMin),
            new Coordinate2D(xMax, yMin),
            new Coordinate2D(xMax, yMax),
            new Coordinate2D(xMin, yMax)
        };

        return PolygonBuilderEx.CreatePolygon(
            coordinates,
            spatialReference);
    }
}
