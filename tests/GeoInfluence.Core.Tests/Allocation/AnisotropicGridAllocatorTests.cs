using GeoInfluence.Core.Allocation;
using GeoInfluence.Core.Models;

namespace GeoInfluence.Core.Tests.Allocation;

public sealed class AnisotropicGridAllocatorTests
{
    [Fact]
    public void Allocate_WithTwoIsotropicSites_SplitsGridByNearestSite()
    {
        var sites = new[]
        {
            new InfluenceSite("A", 0, 0),
            new InfluenceSite("B", 10, 0)
        };

        var grid = new AnisotropicGridAllocator().Allocate(
            sites,
            xMin: 0,
            yMin: -1,
            xMax: 10,
            yMax: 1,
            columns: 10,
            rows: 1);

        Assert.All(grid.Cells.Take(5), cell => Assert.Equal("A", cell.SiteId));
        Assert.All(grid.Cells.Skip(5), cell => Assert.Equal("B", cell.SiteId));
    }

    [Fact]
    public void Allocate_HigherWeight_ExtendsSiteInfluence()
    {
        var sites = new[]
        {
            new InfluenceSite("A", 0, 0, Weight: 2),
            new InfluenceSite("B", 10, 0, Weight: 1)
        };

        var grid = new AnisotropicGridAllocator().Allocate(
            sites,
            xMin: 0,
            yMin: -1,
            xMax: 10,
            yMax: 1,
            columns: 10,
            rows: 1);

        var siteACells = grid.Cells.Count(cell => cell.SiteId == "A");

        Assert.True(siteACells > 5);
    }

    [Fact]
    public void Allocate_AnisotropyChangesWinner()
    {
        var sites = new[]
        {
            new InfluenceSite(
                "A",
                0,
                0,
                Anisotropy: new AnisotropyParameters(
                    BearingDegrees: 90,
                    MajorScale: 4,
                    MinorScale: 1)),
            new InfluenceSite("B", 6, 0)
        };

        var grid = new AnisotropicGridAllocator().Allocate(
            sites,
            xMin: 3.5,
            yMin: -0.5,
            xMax: 4.5,
            yMax: 0.5,
            columns: 1,
            rows: 1);

        Assert.Equal("A", Assert.Single(grid.Cells).SiteId);
    }

    [Fact]
    public void Allocate_StoresRunnerUpMarginAndConfidence()
    {
        var sites = new[]
        {
            new InfluenceSite("A", 0, 0),
            new InfluenceSite("B", 10, 0)
        };

        var grid = new AnisotropicGridAllocator().Allocate(
            sites,
            xMin: 1,
            yMin: -0.5,
            xMax: 2,
            yMax: 0.5,
            columns: 1,
            rows: 1);

        var cell = Assert.Single(grid.Cells);

        Assert.Equal("A", cell.SiteId);
        Assert.True(cell.RunnerUpScore > cell.Score);
        Assert.Equal(
            cell.RunnerUpScore - cell.Score,
            cell.ScoreMargin,
            precision: 10);
        Assert.InRange(cell.Confidence, 0.0, 1.0);
        Assert.True(cell.Confidence > 0.0);
    }

    [Fact]
    public void Allocate_WithSingleSite_HasMaximumConfidence()
    {
        var sites = new[]
        {
            new InfluenceSite("A", 0, 0)
        };

        var grid = new AnisotropicGridAllocator().Allocate(
            sites,
            xMin: 0,
            yMin: 0,
            xMax: 1,
            yMax: 1,
            columns: 1,
            rows: 1);

        var cell = Assert.Single(grid.Cells);

        Assert.True(double.IsPositiveInfinity(cell.RunnerUpScore));
        Assert.True(double.IsPositiveInfinity(cell.ScoreMargin));
        Assert.Equal(1.0, cell.Confidence);
    }

    [Fact]
    public void Allocate_WithInvalidWeight_Throws()
    {
        var sites = new[]
        {
            new InfluenceSite("A", 0, 0, Weight: 0)
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnisotropicGridAllocator().Allocate(
                sites,
                0,
                0,
                10,
                10,
                10,
                10));
    }
}
