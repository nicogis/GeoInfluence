using GeoInfluence.Core.Models;

namespace GeoInfluence.Core.Distance;

public sealed class EuclideanDistanceMetric : IDistanceMetric
{
    public double Calculate(InfluenceSite site, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(site);

        var dx = x - site.X;
        var dy = y - site.Y;

        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
