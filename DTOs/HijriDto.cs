namespace IslamiJindegiApi.DTOs;

public record HijriMonthSightingResponse(
    Guid Id,
    string CountryCode,
    int HijriYear,
    int HijriMonth,
    string MonthNameEn,
    string MonthNameAr,
    string MonthNameBn,
    DateOnly GregorianStartDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateHijriSightingRequest(
    string CountryCode,
    int HijriYear,
    int HijriMonth,
    DateOnly GregorianStartDate);

public record UpdateHijriSightingRequest(
    string CountryCode,
    int HijriYear,
    int HijriMonth,
    DateOnly GregorianStartDate);

// Public API responses
public record HijriDateData(
    int HijriYear,
    int HijriMonth,
    int HijriDay,
    int MonthLength,
    string MonthNameEn,
    string MonthNameAr,
    string MonthNameBn);

public record HijriDateMeta(
    bool Fallback,
    string? Reason,
    string? CountryCode,
    string? ResolvedBy,
    int? OffsetDays,
    DateOnly? SaGregorianStartDate,
    DateOnly? GregorianStartDate,
    DateOnly? NextGregorianStartDate);

// Data is null (and Meta.Fallback true) when the date can't be resolved — clients
// treat that as "use your local fallback", it is not an error condition.
public record HijriDateResponse(HijriDateData? Data, HijriDateMeta Meta);

public record HijriMonthData(
    int HijriYear,
    int HijriMonth,
    int MonthLength,
    string MonthNameEn,
    string MonthNameAr,
    string MonthNameBn,
    DateOnly GregorianStartDate,
    DateOnly NextGregorianStartDate);

public record HijriMonthMeta(
    bool Fallback,
    string? Reason,
    string? CountryCode,
    string? ResolvedBy,
    int? OffsetDays,
    DateOnly? SaGregorianStartDate);

public record HijriMonthResponse(HijriMonthData? Data, HijriMonthMeta Meta);
