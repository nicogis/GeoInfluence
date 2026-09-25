using GeoInfluence.Core.Models;

namespace GeoInfluence.Core.Distance;

/// <summary>
/// Computes the effective distance from a site to a candidate location.
/// </summary>
public interface IDistanceMetric
{
    double Calculate(InfluenceSite site, double x, double y);
}
