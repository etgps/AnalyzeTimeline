namespace AnalyzeTimeline.Application;

public interface IPrefectureResolver
{
    RegionDefinition? Resolve(double latitude, double longitude);
}
