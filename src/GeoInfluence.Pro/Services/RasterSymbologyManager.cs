using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;
using GeoInfluence.Core.Allocation;

namespace GeoInfluence.Pro.Services;

internal static class RasterSymbologyManager
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

    public static bool ApplySiteIdColorizer(
        AllocationGrid grid,
        string rasterName)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentException.ThrowIfNullOrWhiteSpace(rasterName);

        var map = MapView.Active?.Map
            ?? throw new InvalidOperationException("No active map is available.");

        var rasterLayer = map
            .GetLayersAsFlattenedList()
            .OfType<RasterLayer>()
            .FirstOrDefault(layer =>
                string.Equals(
                    layer.Name,
                    rasterName,
                    StringComparison.OrdinalIgnoreCase));

        if (rasterLayer is null)
            return false;

        var colorizer = rasterLayer.CreateColorizer(
            new UniqueValueColorizerDefinition("Value"));

        if (colorizer is not CIMRasterUniqueValueColorizer uniqueValueColorizer ||
            uniqueValueColorizer.Groups is null)
        {
            throw new InvalidOperationException(
                "ArcGIS Pro did not create a unique value raster colorizer.");
        }

        var siteIdByValue = grid.Cells
            .GroupBy(cell => new { cell.SiteIndex, cell.SiteId })
            .ToDictionary(
                group => group.Key.SiteIndex + 1,
                group => group.Key.SiteId);

        foreach (var group in uniqueValueColorizer.Groups)
        {
            if (group.Classes is null)
                continue;

            foreach (var rasterClass in group.Classes)
            {
                var valueText = rasterClass.Values?.FirstOrDefault();
                if (!int.TryParse(valueText, out var rasterValue))
                    continue;

                if (!siteIdByValue.TryGetValue(rasterValue, out var siteId))
                    continue;

                var paletteIndex = (rasterValue - 1) % Palette.Length;
                var color = Palette[paletteIndex];

                rasterClass.Label = siteId;
                rasterClass.Color = ColorFactory.Instance.CreateRGBColor(
                    color.R,
                    color.G,
                    color.B,
                    100);
            }
        }

        uniqueValueColorizer.UseDefaultColor = false;
        rasterLayer.SetColorizer(uniqueValueColorizer);

        return true;
    }
}
