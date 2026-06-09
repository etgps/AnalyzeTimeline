namespace AnalyzeTimeline.Application;

internal sealed class PrefecturePolygon
{
    public PrefecturePolygon(RegionDefinition prefecture, BoundingBox bounds, IReadOnlyList<IReadOnlyList<GeoPoint>> rings)
    {
        Prefecture = prefecture;
        Bounds = bounds;
        Rings = rings;
    }

    public RegionDefinition Prefecture { get; }

    public BoundingBox Bounds { get; }

    public IReadOnlyList<IReadOnlyList<GeoPoint>> Rings { get; }

    public bool Contains(double latitude, double longitude)
    {
        if (!Bounds.Contains(latitude, longitude) || Rings.Count == 0)
        {
            return false;
        }

        if (!ContainsInRing(Rings[0], latitude, longitude))
        {
            return false;
        }

        for (var index = 1; index < Rings.Count; index++)
        {
            if (ContainsInRing(Rings[index], latitude, longitude))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsInRing(IReadOnlyList<GeoPoint> ring, double latitude, double longitude)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var current = ring[i];
            var previous = ring[j];
            var intersects = current.Latitude > latitude != previous.Latitude > latitude &&
                longitude < (previous.Longitude - current.Longitude) * (latitude - current.Latitude) /
                (previous.Latitude - current.Latitude) + current.Longitude;

            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
