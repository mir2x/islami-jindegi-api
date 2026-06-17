using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface INamazTimeService
{
    Task<PagedResult<NamazTimeListItem>> GetListAsync(int page, int pageSize, string? search);
    Task<NamazTimeDetail?> GetByIdAsync(Guid id);
    Task<NamazTimeDetail> CreateAsync(SaveNamazTimeRequest req);
    Task<NamazTimeDetail?> UpdateAsync(Guid id, SaveNamazTimeRequest req);
    Task<bool> DeleteAsync(Guid id);
}
