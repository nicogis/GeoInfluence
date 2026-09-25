namespace GeoInfluence.Core.Models;

/// <summary>
/// Defines an elliptical directional distance model.
/// </summary>
/// <param name="BearingDegrees">
/// Major-axis direction in degrees clockwise from north.
/// </param>
/// <param name="MajorScale">
/// Scale applied along the preferred (major) axis. Must be greater than zero.
/// Values greater than 1 extend influence in this direction.
/// </param>
/// <param name="MinorScale">
/// Scale applied along the perpendicular (minor) axis. Must be greater than zero.
/// </param>
public sealed record AnisotropyParameters(
    double BearingDegrees,
    double MajorScale = 1.0,
    double MinorScale = 1.0)
{
    public void Validate()
    {
        if (!double.IsFinite(BearingDegrees))
            throw new ArgumentOutOfRangeException(nameof(BearingDegrees));

        if (!double.IsFinite(MajorScale) || MajorScale <= 0)
            throw new ArgumentOutOfRangeException(nameof(MajorScale));

        if (!double.IsFinite(MinorScale) || MinorScale <= 0)
            throw new ArgumentOutOfRangeException(nameof(MinorScale));
    }
}
