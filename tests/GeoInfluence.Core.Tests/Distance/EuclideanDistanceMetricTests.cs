using GeoInfluence.Core.Distance;
using GeoInfluence.Core.Models;

namespace GeoInfluence.Core.Tests.Distance;

public sealed class EuclideanDistanceMetricTests
{
    private readonly EuclideanDistanceMetric _metric = new();

    [Fact]
    public void Calculate_WhenCandidateIsSite_ReturnsZero()
    {
        var site = new InfluenceSite("A", 10, 20);

        var distance = _metric.Calculate(site, 10, 20);

        Assert.Equal(0.0, distance);
    }

    [Fact]
    public void Calculate_ForThreeFourFiveTriangle_ReturnsFive()
    {
        var site = new InfluenceSite("A", 0, 0);

        var distance = _metric.Calculate(site, 3, 4);

        Assert.Equal(5.0, distance, 12);
    }

    [Fact]
    public void Calculate_IsSymmetricAroundSite()
    {
        var site = new InfluenceSite("A", 100, 200);

        var first = _metric.Calculate(site, 103, 204);
        var second = _metric.Calculate(site, 97, 196);

        Assert.Equal(first, second, 12);
    }

    [Fact]
    public void Calculate_WithNullSite_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => _metric.Calculate(null!, 0, 0));
    }
}
