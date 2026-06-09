namespace AnalyzeTimeline.Application;

public sealed record RegionDefinition(
    string Code,
    string Name,
    string Group,
    double MinLatitude,
    double MaxLatitude,
    double MinLongitude,
    double MaxLongitude)
{
    public bool Contains(double latitude, double longitude)
    {
        return latitude >= MinLatitude &&
            latitude <= MaxLatitude &&
            longitude >= MinLongitude &&
            longitude <= MaxLongitude;
    }

    public double Area => (MaxLatitude - MinLatitude) * (MaxLongitude - MinLongitude);
}
