namespace AnalyzeTimeline.Domain;

public sealed record VisitRegion(
    string Code,
    string Name,
    string Group,
    DateOnly LastVisitedOn,
    int VisitCount,
    VisitGranularity Granularity);
