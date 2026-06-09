namespace AnalyzeTimeline.Domain;

public sealed record TimelineAnalysisResult(
    IReadOnlyList<VisitRegion> YearlyVisits,
    IReadOnlyList<VisitRegion> MonthlyVisits,
    int ParsedLocationCount,
    int ClassifiedLocationCount);
