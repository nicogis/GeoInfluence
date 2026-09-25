using ArcGIS.Desktop.Mapping;

namespace GeoInfluence.Pro.Models;

public sealed record LayerOption(string Name, FeatureLayer Layer)
{
    public override string ToString() => Name;
}
