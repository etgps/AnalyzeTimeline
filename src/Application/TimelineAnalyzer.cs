using AnalyzeTimeline.Domain;

namespace AnalyzeTimeline.Application;

public sealed class TimelineAnalyzer : ITimelineAnalyzer
{
    private readonly TimelineJsonParser parser = new();
    private readonly IPrefectureResolver prefectureResolver;

    public TimelineAnalyzer(IPrefectureResolver prefectureResolver)
    {
        this.prefectureResolver = prefectureResolver;
    }

    public async Task<TimelineAnalysisResult> AnalyzeAsync(Stream timelineJson, CancellationToken cancellationToken)
    {
        var points = await parser.ParseAsync(timelineJson, cancellationToken);
        var classificationCache = new Dictionary<CoordinateKey, IReadOnlyList<RegionDefinition>>();
        var classified = points
            .SelectMany(point => Classify(point, classificationCache).Select(region => new ClassifiedTimelinePoint(point, region)))
            .ToList();

        var yearly = BuildVisits(classified, VisitGranularity.Year);
        var monthly = BuildVisits(classified, VisitGranularity.Month);

        return new TimelineAnalysisResult(yearly, monthly, points.Count, classified.Count);
    }

    private IReadOnlyList<RegionDefinition> Classify(
        TimelinePoint point,
        Dictionary<CoordinateKey, IReadOnlyList<RegionDefinition>> classificationCache)
    {
        var key = CoordinateKey.From(point);
        if (classificationCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var regions = ClassifyCore(point).ToList();
        classificationCache[key] = regions;
        return regions;
    }

    private IEnumerable<RegionDefinition> ClassifyCore(TimelinePoint point)
    {
        var country = RegionCatalog.Countries
            .Where(region => region.Contains(point.Latitude, point.Longitude))
            .OrderBy(region => region.Area)
            .FirstOrDefault();

        if (country is not null)
        {
            yield return country;
        }

        if (country?.Code == "JP")
        {
            var prefecture = prefectureResolver.Resolve(point.Latitude, point.Longitude);

            if (prefecture is not null)
            {
                yield return prefecture;
            }
        }
    }

    private static IReadOnlyList<VisitRegion> BuildVisits(IEnumerable<ClassifiedTimelinePoint> classified, VisitGranularity granularity)
    {
        return classified
            .GroupBy(item => new
            {
                item.Region.Code,
                item.Region.Name,
                item.Region.Group
            })
            .Select(group => new VisitRegion(
                group.Key.Code,
                group.Key.Name,
                group.Key.Group,
                group.Max(item => DateOnly.FromDateTime(item.Point.VisitedAt.DateTime)),
                group.Count(),
                granularity))
            .OrderByDescending(region => region.LastVisitedOn)
            .ThenBy(region => region.Group)
            .ThenBy(region => region.Name)
            .ToList();
    }
}
