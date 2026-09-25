using ArcGIS.Core.Data;
using ArcGIS.Core.Data.DDL;
using DdlFieldDescription = ArcGIS.Core.Data.DDL.FieldDescription;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using GeoInfluence.Core.Allocation;

namespace GeoInfluence.Pro.Services;

internal static class RasterOutputWriter
{
    internal sealed record PreparationResult(
        string TemporaryFeatureClassPath,
        string OutputRasterPath,
        double CellSize);

    public static PreparationResult Prepare(
        AllocationGrid grid,
        SpatialReference workingSpatialReference)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(workingSpatialReference);

        var geodatabasePath = Project.Current.DefaultGeodatabasePath;
        if (string.IsNullOrWhiteSpace(geodatabasePath))
            throw new InvalidOperationException("The project has no default geodatabase.");

        var suffix = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
        var temporaryFeatureClassName = $"GeoInfluence_RasterCells_{suffix}";
        var outputRasterName = $"GeoInfluence_Raster_{suffix}";

        using var geodatabase = new Geodatabase(
            new FileGeodatabaseConnectionPath(
                new Uri(geodatabasePath)));

        var fields = new List<DdlFieldDescription>
        {
            DdlFieldDescription.CreateIntegerField("SiteIndex"),
            DdlFieldDescription.CreateStringField("SiteId", 128)
        };

        var featureClassDescription = new FeatureClassDescription(
            temporaryFeatureClassName,
            fields,
            new ShapeDescription(
                GeometryType.Polygon,
                workingSpatialReference));

        var schemaBuilder = new SchemaBuilder(geodatabase);
        schemaBuilder.Create(featureClassDescription);

        if (!schemaBuilder.Build())
        {
            var errors = string.Join(
                Environment.NewLine,
                schemaBuilder.ErrorMessages);

            throw new InvalidOperationException(
                $"Unable to create temporary raster source feature class. {errors}");
        }

        using var featureClass =
            geodatabase.OpenDataset<FeatureClass>(temporaryFeatureClassName);

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
                rowBuffer["SiteIndex"] = cell.SiteIndex + 1;
                rowBuffer["SiteId"] = cell.SiteId;

                using var row = featureClass.CreateRow(rowBuffer);
            }
        });

        var temporaryFeatureClassPath =
            Path.Combine(
                geodatabasePath,
                temporaryFeatureClassName);

        var outputRasterPath =
            Path.Combine(
                geodatabasePath,
                outputRasterName);

        var cellSize = Math.Min(
            Math.Abs(grid.CellWidth),
            Math.Abs(grid.CellHeight));

        if (!double.IsFinite(cellSize) || cellSize <= 0)
            throw new InvalidOperationException("The allocation grid has an invalid raster cell size.");

        return new PreparationResult(
            temporaryFeatureClassPath,
            outputRasterPath,
            cellSize);
    }
}
