using AnalyzeTimeline.Domain;

namespace AnalyzeTimeline.Application;

public interface IReportHtmlGenerator
{
    string Generate(TimelineAnalysisResult result);
}
