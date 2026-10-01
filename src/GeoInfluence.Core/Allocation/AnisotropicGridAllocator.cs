using GeoInfluence.Core.Distance;
using GeoInfluence.Core.Models;
using GeoInfluence.Core.Influence;

namespace GeoInfluence.Core.Allocation;

/// <summary>
/// Allocates regular grid cells to the site with the lowest weighted
/// anisotropic distance.
/// </summary>
public sealed class AnisotropicGridAllocator
{
    private readonly IDistanceMetric _distanceMetric;
    private readonly IInfluenceScoreModel _influenceScoreModel;

    public AnisotropicGridAllocator(
        IDistanceMetric? distanceMetric = null,
        IInfluenceScoreModel? influenceScoreModel = null)
    {
        _distanceMetric = distanceMetric ?? new AnisotropicDistanceMetric();
        _influenceScoreModel = influenceScoreModel ?? new WeightedDistanceInfluenceScoreModel();
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
                var winnerScore = CalculateScore(sites[0], centerX, centerY);
                var runnerUpScore = double.PositiveInfinity;

                for (var siteIndex = 1; siteIndex < sites.Count; siteIndex++)
                {
                    var score = CalculateScore(sites[siteIndex], centerX, centerY);

                    if (score < winnerScore)
                    {
                        runnerUpScore = winnerScore;
                        winnerIndex = siteIndex;
                        winnerScore = score;
                    }
                    else if (score < runnerUpScore)
                    {
                        runnerUpScore = score;
                    }
                }

                var scoreMargin = double.IsPositiveInfinity(runnerUpScore)
                    ? double.PositiveInfinity
                    : runnerUpScore - winnerScore;

                var confidence = double.IsPositiveInfinity(runnerUpScore)
                    ? 1.0
                    : runnerUpScore <= 0
                        ? 0.0
                        : Math.Clamp(scoreMargin / runnerUpScore, 0.0, 1.0);

                cells.Add(new AllocationCell(
                    Column: column,
                    Row: row,
                    CenterX: centerX,
                    CenterY: centerY,
                    SiteIndex: winnerIndex,
                    SiteId: sites[winnerIndex].Id,
                    Score: winnerScore,
                    RunnerUpScore: runnerUpScore,
                    ScoreMargin: scoreMargin,
                    Confidence: confidence));
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

    private double CalculateScore(InfluenceSite site, double x, double y)
    {
        var effectiveDistance = _distanceMetric.Calculate(site, x, y);
        var score = _influenceScoreModel.CalculateScore(site, effectiveDistance, x, y);

        if (!double.IsFinite(score) || score < 0)
        {
            throw new InvalidOperationException(
                $"Influence score model returned an invalid score for site '{site.Id}'.");
        }

        return score;
    }
}
