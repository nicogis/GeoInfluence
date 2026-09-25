using ArcGIS.Core.Data;

namespace GeoInfluence.Pro.Models;

public sealed record FieldOption(string Name, string Alias, FieldType FieldType)
{
    public string DisplayName =>
        string.Equals(Name, Alias, StringComparison.Ordinal)
            ? Name
            : $"{Alias} ({Name})";

    public override string ToString() => DisplayName;
}
