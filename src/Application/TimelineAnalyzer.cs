using AnalyzeTimeline.Domain;

namespace AnalyzeTimeline.Application;

public sealed class TimelineAnalyzer : ITimelineAnalyzer
{
    private readonly TimelineJsonParser parser = new();
    private readonly ICountryResolver countryResolver;
    private readonly IPrefectureResolver prefectureResolver;

    public TimelineAnalyzer(ICountryResolver countryResolver, IPrefectureResolver prefectureResolver)
    {
        this.countryResolver = countryResolver;
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
        var country = countryResolver.Resolve(point.Latitude, point.Longitude);

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
                item.Region.Group,
                Year = granularity == VisitGranularity.Month ? item.Point.VisitedAt.Year : 0
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
