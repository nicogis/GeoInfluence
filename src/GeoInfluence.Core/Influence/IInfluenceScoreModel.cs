using GeoInfluence.Core.Models;

namespace GeoInfluence.Core.Influence;

/// <summary>
/// Converts an effective distance into a comparable influence score.
/// Lower scores represent stronger influence.
/// </summary>
public interface IInfluenceScoreModel
{
    double CalculateScore(
        InfluenceSite site,
        double effectiveDistance,
        double x,
        double y);
}
