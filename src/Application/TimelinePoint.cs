namespace AnalyzeTimeline.Application;

internal sealed record TimelinePoint(DateTimeOffset VisitedAt, double Latitude, double Longitude);
