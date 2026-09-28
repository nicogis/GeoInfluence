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
        bool UsedSelection,
        SpatialReference WorkingSpatialReference,
        bool AutoProjected);

    private sealed record RawSite(
        long Oid,
        string Id,
        double Weight,
        double Bearing,
        double MajorScale,
        double MinorScale,
        MapPoint Point);

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

        var mapSpatialReference = MapView.Active?.Map?.SpatialReference
            ?? throw new InvalidOperationException("No active map spatial reference is available.");

        using var selection = layer.GetSelection();
        var useSelection = selection.GetCount() > 0;

        using var cursor = useSelection
            ? selection.Search(null)
            : layer.GetTable().Search(null, false);

        var rawSites = new List<RawSite>();

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

            if (!double.IsFinite(weight) || weight <= 0)
                throw new InvalidOperationException(
                    $"Feature {oid}: field '{weightField.Name}' must contain a finite value greater than zero.");

            if (!double.IsFinite(bearing))
            {
                throw new InvalidOperationException(
                    $"Feature {oid}: field '{bearingField.Name}' must contain a finite bearing value.");
            }

            if (!double.IsFinite(majorScale) || majorScale <= 0)
            {
                throw new InvalidOperationException(
                    $"Feature {oid}: field '{majorScaleField.Name}' must contain a finite value greater than zero.");
            }

            if (!double.IsFinite(minorScale) || minorScale <= 0)
            {
                throw new InvalidOperationException(
                    $"Feature {oid}: field '{minorScaleField.Name}' must contain a finite value greater than zero.");
            }

            var anisotropy = new AnisotropyParameters(
                bearing,
                majorScale,
                minorScale);

            anisotropy.Validate();

            rawSites.Add(new RawSite(
                oid,
                id,
                weight,
                bearing,
                majorScale,
                minorScale,
                point));
        }

        if (rawSites.Count == 0)
        {
            return new ReadResult(
                [],
                useSelection,
                mapSpatialReference,
                false);
        }

        var duplicateIds = rawSites
            .GroupBy(site => site.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            var preview = string.Join(", ", duplicateIds.Take(5));
            var suffix = duplicateIds.Count > 5
                ? $" (+{duplicateIds.Count - 5} more)"
                : string.Empty;

            throw new InvalidOperationException(
                $"Site ID values must be unique. Duplicate ID(s): {preview}{suffix}.");
        }

        var workingSpatialReference = mapSpatialReference;
        var autoProjected = false;

        if (mapSpatialReference.IsGeographic)
        {
            workingSpatialReference = CreateLocalUtmSpatialReference(rawSites);
            autoProjected = true;
        }

        var sites = rawSites
            .Select(raw =>
            {
                var projectedPoint = ProjectPoint(raw.Point, workingSpatialReference);

                return new InfluenceSite(
                    raw.Id,
                    projectedPoint.X,
                    projectedPoint.Y,
                    raw.Weight,
                    new AnisotropyParameters(
                        raw.Bearing,
                        raw.MajorScale,
                        raw.MinorScale));
            })
            .ToList();

        return new ReadResult(
            sites,
            useSelection,
            workingSpatialReference,
            autoProjected);
    }

    private static SpatialReference CreateLocalUtmSpatialReference(
        IReadOnlyList<RawSite> rawSites)
    {
        var wgs84 = SpatialReferenceBuilder.CreateSpatialReference(4326);

        var wgs84Points = rawSites
            .Select(raw => ProjectPoint(raw.Point, wgs84))
            .ToList();

        var longitude = wgs84Points.Average(point => point.X);
        var latitude = wgs84Points.Average(point => point.Y);

        if (latitude < -80 || latitude > 84)
        {
            throw new InvalidOperationException(
                "Automatic metric projection is only supported between 80°S and 84°N. " +
                "Set the map to an appropriate projected coordinate system and reload the sites.");
        }

        var zone = (int)Math.Floor((longitude + 180.0) / 6.0) + 1;
        zone = Math.Clamp(zone, 1, 60);

        var wkid = latitude >= 0
            ? 32600 + zone
            : 32700 + zone;

        return SpatialReferenceBuilder.CreateSpatialReference(wkid);
    }

    private static MapPoint ProjectPoint(
        MapPoint point,
        SpatialReference targetSpatialReference)
    {
        if (point.SpatialReference is null)
        {
            throw new InvalidOperationException(
                "A source point has no spatial reference.");
        }

        if (point.SpatialReference.Wkid == targetSpatialReference.Wkid)
            return point;

        return GeometryEngine.Instance.Project(
                   point,
                   targetSpatialReference) as MapPoint
               ?? throw new InvalidOperationException(
                   "Unable to project a source point to the working coordinate system.");
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
