namespace AnalyzeTimeline.Api;

public sealed record AnalyzeResponse(
    int ParsedLocationCount,
    int ClassifiedLocationCount,
    int YearlyVisitCount,
    int MonthlyVisitCount,
    string Html);
