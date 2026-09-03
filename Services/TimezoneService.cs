using GeoTimeZone;
using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

/// Resolves a coordinate to its IANA time zone from the timezone-boundary-builder
/// polygons bundled in GeoTimeZone.
///
/// This exists because a country code is not enough to place a clock: 24 of the
/// world's countries span more than one UTC offset, so picking a country's
/// "first" zone puts Los Angeles on America/Adak (2h out) and New York on the
/// same zone (5h out). Prayer times are computed from the coordinate, so the
/// offset has to come from the coordinate too.
///
/// Deliberately a singleton with no dependencies: the lookup is a pure function
/// over embedded data with no I/O, so it is safe to share and cheap to call.
public class TimezoneService : ITimezoneService
{
    public (TimezoneResponse? Result, string? Error) Resolve(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || double.IsNaN(longitude) ||
            double.IsInfinity(latitude) || double.IsInfinity(longitude))
            return (null, "lat and lng must be finite numbers");

        if (latitude is < -90 or > 90)
            return (null, "lat must be between -90 and 90");

        if (longitude is < -180 or > 180)
            return (null, "lng must be between -180 and 180");

        var zoneId = TimeZoneLookup.GetTimeZone(latitude, longitude).Result;

        // Open ocean and a handful of unmapped points return nothing. Reporting
        // that plainly lets the caller fall back to its own offline lookup
        // rather than silently accepting a zone that was never resolved.
        if (string.IsNullOrWhiteSpace(zoneId))
            return (new TimezoneResponse(
                null,
                new TimezoneMeta(true, "no time zone polygon covers this coordinate")), null);

        var (offsetSeconds, isDst) = DescribeOffset(zoneId);

        return (new TimezoneResponse(
            new TimezoneData(zoneId, latitude, longitude, offsetSeconds, isDst),
            new TimezoneMeta(false, null)), null);
    }

    // The runtime resolves IANA ids against the host's tzdata. That is present
    // on the Debian-based aspnet image this deploys to, but a distroless or
    // Alpine base would drop it — in which case the zone id still stands on its
    // own and only these two advisory fields go null.
    static (int? OffsetSeconds, bool? IsDst) DescribeOffset(string zoneId)
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
            var now = DateTime.UtcNow;
            return ((int)zone.GetUtcOffset(now).TotalSeconds, zone.IsDaylightSavingTime(
                TimeZoneInfo.ConvertTimeFromUtc(now, zone)));
        }
        catch (TimeZoneNotFoundException)
        {
            return (null, null);
        }
        catch (InvalidTimeZoneException)
        {
            return (null, null);
        }
    }
}
