namespace AnalyzeTimeline.Application;

public sealed class BoundingBoxCountryResolver : ICountryResolver
{
    public RegionDefinition? Resolve(double latitude, double longitude)
    {
        return RegionCatalog.Countries
            .Where(region => region.Contains(latitude, longitude))
            .OrderBy(region => region.Area)
            .FirstOrDefault();
    }
}
