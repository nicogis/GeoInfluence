namespace GeoInfluence.Core.Models;

/// <summary>
/// A source feature that participates in an influence allocation.
/// Coordinates are expressed in the working coordinate system used by the engine.
/// </summary>
public sealed record InfluenceSite(
    string Id,
    double X,
    double Y,
    double Weight = 1.0,
    AnisotropyParameters? Anisotropy = null);
