using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IHijriService
{
    Task<(HijriDateResponse? Result, string? Error)> GetDateAsync(string countryCode, string? date);
    Task<(HijriMonthResponse? Result, string? Error)> GetMonthAsync(string countryCode, int hijriYear, int hijriMonth);
    Task<PagedResult<HijriMonthSightingResponse>> GetSightingsAsync(int page, int pageSize, string? countryCode, int? hijriYear);
    Task<HijriMonthSightingResponse?> GetSightingByIdAsync(Guid id);
    Task<(HijriMonthSightingResponse? Item, string? Error)> CreateSightingAsync(CreateHijriSightingRequest req);
    Task<(HijriMonthSightingResponse? Item, string? Error)> UpdateSightingAsync(Guid id, UpdateHijriSightingRequest req);
    Task<bool> DeleteSightingAsync(Guid id);
}
