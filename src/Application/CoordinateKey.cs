namespace AnalyzeTimeline.Application;

internal readonly record struct CoordinateKey(double Latitude, double Longitude)
{
    public static CoordinateKey From(TimelinePoint point)
    {
        return new CoordinateKey(Math.Round(point.Latitude, 6), Math.Round(point.Longitude, 6));
    }
}
