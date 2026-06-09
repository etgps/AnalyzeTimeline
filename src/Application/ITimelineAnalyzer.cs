using AnalyzeTimeline.Domain;

namespace AnalyzeTimeline.Application;

public interface ITimelineAnalyzer
{
    Task<TimelineAnalysisResult> AnalyzeAsync(Stream timelineJson, CancellationToken cancellationToken);
}
