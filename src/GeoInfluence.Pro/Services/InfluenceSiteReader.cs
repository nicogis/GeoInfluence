using System.Globalization;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using GeoInfluence.Core.Models;
using GeoInfluence.Pro.Models;

namespace GeoInfluence.Pro.Services;

internal static class InfluenceSiteReader
{
    internal sealed record ReadResult(
        IReadOnlyList<InfluenceSite> Sites,
        bool UsedSelection);

    public static ReadResult Read(
        FeatureLayer layer,
        FieldOption idField,
        FieldOption weightField,
        FieldOption bearingField,
        FieldOption majorScaleField,
        FieldOption minorScaleField)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(idField);
        ArgumentNullException.ThrowIfNull(weightField);
        ArgumentNullException.ThrowIfNull(bearingField);
        ArgumentNullException.ThrowIfNull(majorScaleField);
        ArgumentNullException.ThrowIfNull(minorScaleField);

        using var selection = layer.GetSelection();
        var useSelection = selection.GetCount() > 0;

        using var cursor = useSelection
            ? selection.Search(null)
            : layer.GetTable().Search(null, false);

        var sites = new List<InfluenceSite>();

        while (cursor.MoveNext())
        {
            using var row = cursor.Current;

            if (row is not Feature feature)
                continue;

            if (feature.GetShape() is not MapPoint point)
                continue;

            var oid = feature.GetObjectID();

            var id = ReadId(row, idField.Name, oid);
            var weight = ReadDouble(row, weightField.Name, oid);
            var bearing = ReadDouble(row, bearingField.Name, oid);
            var majorScale = ReadDouble(row, majorScaleField.Name, oid);
            var minorScale = ReadDouble(row, minorScaleField.Name, oid);

            if (!double.IsFinite(weight))
                throw new InvalidOperationException(
                    $"Feature {oid}: field '{weightField.Name}' must contain a finite number.");

            var anisotropy = new AnisotropyParameters(
                bearing,
                majorScale,
                minorScale);

            anisotropy.Validate();

            sites.Add(new InfluenceSite(
                id,
                point.X,
                point.Y,
                weight,
                anisotropy));
        }

        return new ReadResult(sites, useSelection);
    }

    private static string ReadId(Row row, string fieldName, long oid)
    {
        var value = row[fieldName];

        if (value is null or DBNull)
            return oid.ToString(CultureInfo.InvariantCulture);

        var text = Convert.ToString(value, CultureInfo.InvariantCulture);

        return string.IsNullOrWhiteSpace(text)
            ? oid.ToString(CultureInfo.InvariantCulture)
            : text;
    }

    private static double ReadDouble(Row row, string fieldName, long oid)
    {
        var value = row[fieldName];

        if (value is null or DBNull)
            throw new InvalidOperationException(
                $"Feature {oid}: field '{fieldName}' is null.");

        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            throw new InvalidOperationException(
                $"Feature {oid}: field '{fieldName}' does not contain a valid numeric value.",
                ex);
        }
    }
}
