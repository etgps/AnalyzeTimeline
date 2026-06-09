namespace AnalyzeTimeline.Application;

internal readonly record struct BoundingBox(double MinLatitude, double MaxLatitude, double MinLongitude, double MaxLongitude)
{
    public bool Contains(double latitude, double longitude)
    {
        return latitude >= MinLatitude &&
            latitude <= MaxLatitude &&
            longitude >= MinLongitude &&
            longitude <= MaxLongitude;
    }
}
