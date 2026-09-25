using GeoInfluence.Core.Distance;
using GeoInfluence.Core.Models;

namespace GeoInfluence.Core.Allocation;

/// <summary>
/// Allocates regular grid cells to the site with the lowest weighted
/// anisotropic distance.
/// </summary>
public sealed class AnisotropicGridAllocator
{
    private readonly IDistanceMetric _distanceMetric;

    public AnisotropicGridAllocator(IDistanceMetric? distanceMetric = null)
    {
        _distanceMetric = distanceMetric ?? new AnisotropicDistanceMetric();
    }

    public AllocationGrid Allocate(
        IReadOnlyList<InfluenceSite> sites,
        double xMin,
        double yMin,
        double xMax,
        double yMax,
        int columns,
        int rows)
    {
        ArgumentNullException.ThrowIfNull(sites);

        if (sites.Count == 0)
            throw new ArgumentException("At least one influence site is required.", nameof(sites));

        if (!double.IsFinite(xMin) ||
            !double.IsFinite(yMin) ||
            !double.IsFinite(xMax) ||
            !double.IsFinite(yMax) ||
            xMax <= xMin ||
            yMax <= yMin)
        {
            throw new ArgumentOutOfRangeException(nameof(xMax), "A finite, non-empty allocation extent is required.");
        }

        if (columns <= 0)
            throw new ArgumentOutOfRangeException(nameof(columns));

        if (rows <= 0)
            throw new ArgumentOutOfRangeException(nameof(rows));

        foreach (var site in sites)
        {
            if (!double.IsFinite(site.Weight) || site.Weight <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sites),
                    $"Site '{site.Id}' must have a finite Weight greater than zero.");
            }
        }

        var cellWidth = (xMax - xMin) / columns;
        var cellHeight = (yMax - yMin) / rows;
        var cells = new List<AllocationCell>(columns * rows);

        for (var row = 0; row < rows; row++)
        {
            var centerY = yMin + ((row + 0.5) * cellHeight);

            for (var column = 0; column < columns; column++)
            {
                var centerX = xMin + ((column + 0.5) * cellWidth);

                var winnerIndex = 0;
                var winnerScore = WeightedScore(sites[0], centerX, centerY);

                for (var siteIndex = 1; siteIndex < sites.Count; siteIndex++)
                {
                    var score = WeightedScore(sites[siteIndex], centerX, centerY);

                    if (score < winnerScore)
                    {
                        winnerIndex = siteIndex;
                        winnerScore = score;
                    }
                }

                cells.Add(new AllocationCell(
                    Column: column,
                    Row: row,
                    CenterX: centerX,
                    CenterY: centerY,
                    SiteIndex: winnerIndex,
                    SiteId: sites[winnerIndex].Id,
                    Score: winnerScore));
            }
        }

        return new AllocationGrid(
            XMin: xMin,
            YMin: yMin,
            XMax: xMax,
            YMax: yMax,
            Columns: columns,
            Rows: rows,
            CellWidth: cellWidth,
            CellHeight: cellHeight,
            Cells: cells);
    }

    private double WeightedScore(InfluenceSite site, double x, double y)
    {
        return _distanceMetric.Calculate(site, x, y) / site.Weight;
    }
}
