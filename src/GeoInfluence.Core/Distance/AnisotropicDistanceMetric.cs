using GeoInfluence.Core.Models;

namespace GeoInfluence.Core.Distance;

/// <summary>
/// Computes an elliptical anisotropic distance.
///
/// Bearing follows GIS convention: degrees clockwise from north.
/// MajorScale and MinorScale stretch the site's influence ellipse:
/// a larger scale produces a smaller effective distance along that axis.
/// </summary>
public sealed class AnisotropicDistanceMetric : IDistanceMetric
{
    public double Calculate(InfluenceSite site, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(site);

        var deltaX = x - site.X;
        var deltaY = y - site.Y;

        var anisotropy = site.Anisotropy;
        if (anisotropy is null)
        {
            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }

        anisotropy.Validate();

        // Convert GIS bearing (clockwise from north) to a standard
        // mathematical angle (counter-clockwise from +X).
        var angleRadians = (90.0 - anisotropy.BearingDegrees) * Math.PI / 180.0;
        var cos = Math.Cos(angleRadians);
        var sin = Math.Sin(angleRadians);

        // Rotate the candidate vector into the site's local major/minor axes.
        var major = (deltaX * cos) + (deltaY * sin);
        var minor = (-deltaX * sin) + (deltaY * cos);

        var normalizedMajor = major / anisotropy.MajorScale;
        var normalizedMinor = minor / anisotropy.MinorScale;

        return Math.Sqrt(
            (normalizedMajor * normalizedMajor) +
            (normalizedMinor * normalizedMinor));
    }
}
