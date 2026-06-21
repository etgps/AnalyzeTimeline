namespace AnalyzeTimeline.Application;

public interface ICountryResolver
{
    RegionDefinition? Resolve(double latitude, double longitude);
}
