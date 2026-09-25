using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using GeoInfluence.Core.Allocation;

namespace GeoInfluence.Pro.Services;

internal static class PreviewOverlayManager
{
    private static readonly List<IDisposable> Graphics = [];

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

    public static void Render(
        AllocationGrid grid,
        SpatialReference workingSpatialReference)
    {
        ArgumentNullException.ThrowIfNull(workingSpatialReference);

        Clear();

        var mapView = MapView.Active
            ?? throw new InvalidOperationException("No active map view is available.");

        var mapSpatialReference = mapView.Map.SpatialReference;
        var symbols = BuildSymbols(grid.Cells.Max(cell => cell.SiteIndex) + 1);

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

            var workingPolygon = PolygonBuilderEx.CreatePolygon(
                coordinates,
                workingSpatialReference);

            var displayPolygon =
                workingSpatialReference.Wkid == mapSpatialReference.Wkid
                    ? workingPolygon
                    : GeometryEngine.Instance.Project(
                          workingPolygon,
                          mapSpatialReference) as Polygon
                      ?? throw new InvalidOperationException(
                          "Unable to project an allocation cell back to the map spatial reference.");

            Graphics.Add(
                MappingExtensions.AddOverlay(
                    mapView,
                    displayPolygon,
                    symbols[cell.SiteIndex]));
        }
    }

    public static void Clear()
    {
        foreach (var graphic in Graphics)
            graphic.Dispose();

        Graphics.Clear();
    }

    private static CIMSymbolReference[] BuildSymbols(int count)
    {
        var symbols = new CIMSymbolReference[count];

        for (var index = 0; index < count; index++)
        {
            var color = Palette[index % Palette.Length];
            var fill = ColorFactory.Instance.CreateRGBColor(
                color.R,
                color.G,
                color.B,
                75);

            var outline = SymbolFactory.Instance.ConstructStroke(
                ColorFactory.Instance.CreateRGBColor(255, 255, 255, 35),
                0.25,
                SimpleLineStyle.Solid);

            symbols[index] = SymbolFactory.Instance
                .ConstructPolygonSymbol(fill, SimpleFillStyle.Solid, outline)
                .MakeSymbolReference();
        }

        return symbols;
    }
}
