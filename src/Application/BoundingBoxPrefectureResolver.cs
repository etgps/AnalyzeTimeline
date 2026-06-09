namespace AnalyzeTimeline.Application;

public sealed class BoundingBoxPrefectureResolver : IPrefectureResolver
{
    public RegionDefinition? Resolve(double latitude, double longitude)
    {
        return RegionCatalog.JapanesePrefectures
            .Where(region => region.Contains(latitude, longitude))
            .OrderBy(region => region.Area)
            .FirstOrDefault();
    }
}
