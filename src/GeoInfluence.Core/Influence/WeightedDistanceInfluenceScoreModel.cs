using GeoInfluence.Core.Models;

namespace GeoInfluence.Core.Influence;

/// <summary>
/// Preserves the GeoInfluence 0.1 scoring model:
/// Score = EffectiveDistance / Weight.
/// </summary>
public sealed class WeightedDistanceInfluenceScoreModel : IInfluenceScoreModel
{
    public double CalculateScore(
        InfluenceSite site,
        double effectiveDistance,
        double x,
        double y)
    {
        ArgumentNullException.ThrowIfNull(site);

        if (!double.IsFinite(effectiveDistance) || effectiveDistance < 0)
            throw new ArgumentOutOfRangeException(nameof(effectiveDistance));

        if (!double.IsFinite(site.Weight) || site.Weight <= 0)
            throw new ArgumentOutOfRangeException(nameof(site), "Site Weight must be finite and greater than zero.");

        return effectiveDistance / site.Weight;
    }
}
