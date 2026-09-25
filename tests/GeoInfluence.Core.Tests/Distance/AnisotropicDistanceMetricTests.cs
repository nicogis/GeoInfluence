using GeoInfluence.Core.Distance;
using GeoInfluence.Core.Models;

namespace GeoInfluence.Core.Tests.Distance;

public sealed class AnisotropicDistanceMetricTests
{
    private readonly AnisotropicDistanceMetric _metric = new();

    [Fact]
    public void Calculate_WithoutAnisotropy_EqualsEuclideanDistance()
    {
        var site = new InfluenceSite("A", 0, 0);

        var distance = _metric.Calculate(site, 3, 4);

        Assert.Equal(5.0, distance, 12);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(45.0)]
    [InlineData(90.0)]
    [InlineData(270.0)]
    public void Calculate_WithUnitScales_EqualsEuclideanDistance(double bearing)
    {
        var site = new InfluenceSite(
            "A",
            0,
            0,
            Anisotropy: new AnisotropyParameters(bearing, 1, 1));

        var distance = _metric.Calculate(site, 3, 4);

        Assert.Equal(5.0, distance, 12);
    }

    [Fact]
    public void Calculate_BearingNinety_MajorAxisRunsEastWest()
    {
        var site = new InfluenceSite(
            "A",
            0,
            0,
            Anisotropy: new AnisotropyParameters(
                BearingDegrees: 90,
                MajorScale: 4,
                MinorScale: 1));

        var east = _metric.Calculate(site, 4, 0);
        var north = _metric.Calculate(site, 0, 4);

        Assert.Equal(1.0, east, 12);
        Assert.Equal(4.0, north, 12);
    }

    [Fact]
    public void Calculate_BearingZero_MajorAxisRunsNorthSouth()
    {
        var site = new InfluenceSite(
            "A",
            0,
            0,
            Anisotropy: new AnisotropyParameters(
                BearingDegrees: 0,
                MajorScale: 4,
                MinorScale: 1));

        var north = _metric.Calculate(site, 0, 4);
        var east = _metric.Calculate(site, 4, 0);

        Assert.Equal(1.0, north, 12);
        Assert.Equal(4.0, east, 12);
    }

    [Fact]
    public void Calculate_OppositeDirectionsOnSameAxis_AreSymmetric()
    {
        var site = new InfluenceSite(
            "A",
            10,
            20,
            Anisotropy: new AnisotropyParameters(
                BearingDegrees: 30,
                MajorScale: 3,
                MinorScale: 0.5));

        var first = _metric.Calculate(site, 13, 24);
        var second = _metric.Calculate(site, 7, 16);

        Assert.Equal(first, second, 12);
    }

    [Fact]
    public void Calculate_BearingZeroAndThreeSixty_AreEquivalent()
    {
        var zero = new InfluenceSite(
            "A",
            0,
            0,
            Anisotropy: new AnisotropyParameters(0, 3, 0.75));

        var fullTurn = new InfluenceSite(
            "B",
            0,
            0,
            Anisotropy: new AnisotropyParameters(360, 3, 0.75));

        var first = _metric.Calculate(zero, 7, 11);
        var second = _metric.Calculate(fullTurn, 7, 11);

        Assert.Equal(first, second, 12);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(-1.0, 1.0)]
    [InlineData(1.0, 0.0)]
    [InlineData(1.0, -1.0)]
    public void Calculate_WithNonPositiveScale_Throws(
        double majorScale,
        double minorScale)
    {
        var site = new InfluenceSite(
            "A",
            0,
            0,
            Anisotropy: new AnisotropyParameters(
                BearingDegrees: 0,
                MajorScale: majorScale,
                MinorScale: minorScale));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => _metric.Calculate(site, 1, 1));
    }

    [Fact]
    public void Calculate_WithNonFiniteBearing_Throws()
    {
        var site = new InfluenceSite(
            "A",
            0,
            0,
            Anisotropy: new AnisotropyParameters(double.NaN, 1, 1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => _metric.Calculate(site, 1, 1));
    }

    [Fact]
    public void Calculate_WithNullSite_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => _metric.Calculate(null!, 0, 0));
    }
}
