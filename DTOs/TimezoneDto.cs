namespace IslamiJindegiApi.DTOs;

// The IANA zone id is the whole answer — clients carry a full tz database and
// derive their own DST-aware offsets from it. `UtcOffsetSeconds`/`IsDst` are
// a convenience for callers that don't (and diagnostics), and are null when
// the host image ships without a tzdata directory.
public record TimezoneData(
    string TimeZoneId,
    double Latitude,
    double Longitude,
    int? UtcOffsetSeconds,
    bool? IsDaylightSavingTime);

// Data is null (and Fallback true) when no land polygon covers the point —
// clients treat that as "use your own offline lookup", it is not an error.
public record TimezoneMeta(bool Fallback, string? Reason);

public record TimezoneResponse(TimezoneData? Data, TimezoneMeta Meta);
