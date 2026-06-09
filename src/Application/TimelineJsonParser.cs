using System.Globalization;
using System.Text.Json;

namespace AnalyzeTimeline.Application;

internal sealed class TimelineJsonParser
{
    public async Task<IReadOnlyList<TimelinePoint>> ParseAsync(Stream jsonStream, CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellationToken);
        var points = ParseSemanticVisits(document.RootElement);

        if (points.Count == 0)
        {
            Walk(document.RootElement, null, points);
        }

        return points
            .GroupBy(point => new
            {
                Instant = point.VisitedAt.ToUnixTimeSeconds() / 60,
                Lat = Math.Round(point.Latitude, 4),
                Lng = Math.Round(point.Longitude, 4)
            })
            .Select(group => group.First())
            .OrderBy(point => point.VisitedAt)
            .ToList();
    }

    private static List<TimelinePoint> ParseSemanticVisits(JsonElement root)
    {
        var points = new List<TimelinePoint>();

        if (!root.TryGetProperty("semanticSegments", out var segments) || segments.ValueKind != JsonValueKind.Array)
        {
            return points;
        }

        foreach (var segment in segments.EnumerateArray())
        {
            if (segment.ValueKind != JsonValueKind.Object || !segment.TryGetProperty("visit", out var visit))
            {
                continue;
            }

            var visitedAt = TryReadTime(segment);
            if (visitedAt is null)
            {
                continue;
            }

            if (TryReadVisitCoordinate(visit, out var latitude, out var longitude))
            {
                points.Add(new TimelinePoint(visitedAt.Value, latitude, longitude));
            }
        }

        return points;
    }

    private static bool TryReadVisitCoordinate(JsonElement visit, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;

        if (visit.TryGetProperty("topCandidate", out var topCandidate) &&
            TryReadCandidateCoordinate(topCandidate, out latitude, out longitude))
        {
            return true;
        }

        if (visit.TryGetProperty("candidate", out var candidate) &&
            TryReadCandidateCoordinate(candidate, out latitude, out longitude))
        {
            return true;
        }

        if (visit.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in candidates.EnumerateArray())
            {
                if (TryReadCandidateCoordinate(item, out latitude, out longitude))
                {
                    return true;
                }
            }
        }

        return TryReadCoordinate(visit, out latitude, out longitude);
    }

    private static bool TryReadCandidateCoordinate(JsonElement candidate, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;

        if (candidate.TryGetProperty("placeLocation", out var placeLocation))
        {
            if (placeLocation.ValueKind == JsonValueKind.String &&
                TryParseGeoText(placeLocation.GetString(), out latitude, out longitude))
            {
                return true;
            }

            if (placeLocation.ValueKind == JsonValueKind.Object &&
                TryReadCoordinate(placeLocation, out latitude, out longitude))
            {
                return true;
            }
        }

        return TryReadCoordinate(candidate, out latitude, out longitude);
    }

    private static void Walk(JsonElement element, DateTimeOffset? inheritedTime, List<TimelinePoint> points)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var currentTime = TryReadTime(element) ?? inheritedTime;

            if (TryReadCoordinate(element, out var latitude, out var longitude) && currentTime is not null)
            {
                points.Add(new TimelinePoint(currentTime.Value, latitude, longitude));
            }

            foreach (var property in element.EnumerateObject())
            {
                Walk(property.Value, currentTime, points);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                Walk(item, inheritedTime, points);
            }
        }
    }

    private static DateTimeOffset? TryReadTime(JsonElement element)
    {
        foreach (var name in new[] { "startTime", "endTime", "time", "timestamp", "timestampMs" })
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
                {
                    return parsed;
                }

                if (long.TryParse(text, out var epochText))
                {
                    return FromEpoch(epochText);
                }
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var epoch))
            {
                return FromEpoch(epoch);
            }
        }

        return null;
    }

    private static DateTimeOffset FromEpoch(long value)
    {
        return value > 10_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(value)
            : DateTimeOffset.FromUnixTimeSeconds(value);
    }

    private static bool TryReadCoordinate(JsonElement element, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;

        if (TryReadNumber(element, "latitudeE7", out var latitudeE7) &&
            TryReadNumber(element, "longitudeE7", out var longitudeE7))
        {
            latitude = latitudeE7 / 10_000_000d;
            longitude = longitudeE7 / 10_000_000d;
            return IsValidCoordinate(latitude, longitude);
        }

        if ((TryReadNumber(element, "latitude", out latitude) || TryReadNumber(element, "lat", out latitude)) &&
            (TryReadNumber(element, "longitude", out longitude) || TryReadNumber(element, "lng", out longitude) || TryReadNumber(element, "lon", out longitude)))
        {
            return IsValidCoordinate(latitude, longitude);
        }

        foreach (var propertyName in new[] { "placeLocation", "location", "point", "geo", "latLng" })
        {
            if (element.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                TryParseGeoText(value.GetString(), out latitude, out longitude))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadNumber(JsonElement element, string propertyName, out double value)
    {
        value = 0;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number)
        {
            return property.TryGetDouble(out value);
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        return false;
    }

    private static bool TryParseGeoText(string? text, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text
            .Replace("geo:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("°", string.Empty, StringComparison.OrdinalIgnoreCase);

        var parts = normalized.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 &&
            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude) &&
            IsValidCoordinate(latitude, longitude);
    }

    private static bool IsValidCoordinate(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
    }
}
