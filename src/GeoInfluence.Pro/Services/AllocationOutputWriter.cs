using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using GeoInfluence.Core.Allocation;

namespace GeoInfluence.Pro.Services;

internal static class AllocationOutputWriter
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

        var featureClassName = $"GeoInfluence_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

        using var geodatabase = new Geodatabase(
            new FileGeodatabaseConnectionPath(
                new Uri(geodatabasePath)));

        var fields = new List<FieldDescription>
        {
            FieldDescription.CreateStringField("SiteId", 128),
            new("Score", FieldType.Double),
            FieldDescription.CreateIntegerField("GridRow"),
            FieldDescription.CreateIntegerField("GridCol")
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

                var coordinates = new[]
                {
                    new Coordinate2D(xMin, yMin),
                    new Coordinate2D(xMax, yMin),
                    new Coordinate2D(xMax, yMax),
                    new Coordinate2D(xMin, yMax)
                };

                var polygon = PolygonBuilderEx.CreatePolygon(
                    coordinates,
                    workingSpatialReference);

                using var rowBuffer = featureClass.CreateRowBuffer();
                rowBuffer[shapeField] = polygon;
                rowBuffer["SiteId"] = cell.SiteId;
                rowBuffer["Score"] = cell.Score;
                rowBuffer["GridRow"] = cell.Row;
                rowBuffer["GridCol"] = cell.Column;

                using var row = featureClass.CreateRow(rowBuffer);
            }
        });

        LayerFactory.Instance.CreateLayer<FeatureLayer>(
            new FeatureLayerCreationParams(featureClass)
            {
                Name = featureClassName
            },
            map);

        return new OutputResult(
            featureClassName,
            geodatabasePath,
            grid.Cells.Count);
    }
}
