namespace AnalyzeTimeline.Application;

internal sealed class PrefecturePolygonIndex
{
    private const double CellSize = 0.1d;
    private readonly Dictionary<long, List<PrefecturePolygon>> cells = [];

    public PrefecturePolygonIndex(IEnumerable<PrefecturePolygon> polygons)
    {
        foreach (var polygon in polygons)
        {
            Add(polygon);
        }
    }

    public RegionDefinition? Resolve(double latitude, double longitude)
    {
        var key = BuildKey(ToCell(latitude), ToCell(longitude));
        if (!cells.TryGetValue(key, out var candidates))
        {
            return null;
        }

        foreach (var polygon in candidates)
        {
            if (polygon.Contains(latitude, longitude))
            {
                return polygon.Prefecture;
            }
        }

        return null;
    }

    private void Add(PrefecturePolygon polygon)
    {
        var minLatitudeCell = ToCell(polygon.Bounds.MinLatitude);
        var maxLatitudeCell = ToCell(polygon.Bounds.MaxLatitude);
        var minLongitudeCell = ToCell(polygon.Bounds.MinLongitude);
        var maxLongitudeCell = ToCell(polygon.Bounds.MaxLongitude);

        for (var latitudeCell = minLatitudeCell; latitudeCell <= maxLatitudeCell; latitudeCell++)
        {
            for (var longitudeCell = minLongitudeCell; longitudeCell <= maxLongitudeCell; longitudeCell++)
            {
                var key = BuildKey(latitudeCell, longitudeCell);
                if (!cells.TryGetValue(key, out var polygons))
                {
                    polygons = [];
                    cells[key] = polygons;
                }

                polygons.Add(polygon);
            }
        }
    }

    private static int ToCell(double value)
    {
        return (int)Math.Floor(value / CellSize);
    }

    private static long BuildKey(int latitudeCell, int longitudeCell)
    {
        return ((long)latitudeCell << 32) ^ (uint)longitudeCell;
    }
}
