using System.Text.Json;

namespace AnalyzeTimeline.Application;

public sealed class GeoJsonCountryResolver : ICountryResolver
{
    private const string DefaultPath = "data/natural_earth_countries.geojson";
    private readonly Lazy<PrefecturePolygonIndex?> polygonIndex;

    public GeoJsonCountryResolver()
    {
        polygonIndex = new Lazy<PrefecturePolygonIndex?>(LoadPolygonIndex, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public RegionDefinition? Resolve(double latitude, double longitude)
    {
        var country = polygonIndex.Value?.Resolve(latitude, longitude);
        if (country is not null)
        {
            return country;
        }

        return RegionCatalog.Countries
            .Where(region => region.Contains(latitude, longitude))
            .OrderBy(region => region.Area)
            .FirstOrDefault();
    }

    private static PrefecturePolygonIndex? LoadPolygonIndex()
    {
        var dataPath = ResolveDataPath();
        if (dataPath is null)
        {
            return null;
        }

        using var stream = File.OpenRead(dataPath);
        using var document = JsonDocument.Parse(stream);
        var result = new List<PrefecturePolygon>();

        if (!document.RootElement.TryGetProperty("features", out var features) ||
            features.ValueKind != JsonValueKind.Array)
        {
            return new PrefecturePolygonIndex(result);
        }

        foreach (var feature in features.EnumerateArray())
        {
            var country = TryReadCountry(feature);
            if (country is null ||
                !feature.TryGetProperty("geometry", out var geometry) ||
                !geometry.TryGetProperty("type", out var typeElement) ||
                typeElement.GetString() is not "MultiPolygon" and not "Polygon" ||
                !geometry.TryGetProperty("coordinates", out var coordinates))
            {
                continue;
            }

            if (typeElement.GetString() == "Polygon")
            {
                AddPolygon(result, country, coordinates);
            }
            else
            {
                foreach (var polygon in coordinates.EnumerateArray())
                {
                    AddPolygon(result, country, polygon);
                }
            }
        }

        return new PrefecturePolygonIndex(result);
    }

    private static RegionDefinition? TryReadCountry(JsonElement feature)
    {
        if (!feature.TryGetProperty("properties", out var properties) ||
            !properties.TryGetProperty("code", out var codeElement))
        {
            return null;
        }

        var code = codeElement.GetString();
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var name = properties.TryGetProperty("name", out var nameElement)
            ? nameElement.GetString()
            : code;

        return new RegionDefinition(code, name ?? code, "World", -90, 90, -180, 180);
    }

    private static void AddPolygon(List<PrefecturePolygon> result, RegionDefinition country, JsonElement polygon)
    {
        var rings = new List<IReadOnlyList<GeoPoint>>();
        var minLatitude = double.MaxValue;
        var maxLatitude = double.MinValue;
        var minLongitude = double.MaxValue;
        var maxLongitude = double.MinValue;

        foreach (var ringElement in polygon.EnumerateArray())
        {
            var ring = new List<GeoPoint>();
            foreach (var pointElement in ringElement.EnumerateArray())
            {
                if (pointElement.GetArrayLength() < 2)
                {
                    continue;
                }

                var longitude = pointElement[0].GetDouble();
                var latitude = pointElement[1].GetDouble();
                ring.Add(new GeoPoint(latitude, longitude));

                minLatitude = Math.Min(minLatitude, latitude);
                maxLatitude = Math.Max(maxLatitude, latitude);
                minLongitude = Math.Min(minLongitude, longitude);
                maxLongitude = Math.Max(maxLongitude, longitude);
            }

            if (ring.Count >= 4)
            {
                rings.Add(ring);
            }
        }

        if (rings.Count > 0)
        {
            result.Add(new PrefecturePolygon(
                country,
                new BoundingBox(minLatitude, maxLatitude, minLongitude, maxLongitude),
                rings));
        }
    }

    private static string? ResolveDataPath()
    {
        foreach (var root in EnumerateSearchRoots())
        {
            var path = Path.Combine(root, DefaultPath);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }

        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (baseDirectory is not null)
        {
            yield return baseDirectory.FullName;
            baseDirectory = baseDirectory.Parent;
        }
    }
}
