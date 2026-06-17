using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IMadrasahService
{
    Task<PagedResult<MadrasahListItem>> GetListAsync(int page, int pageSize, string? search);
    Task<MadrasahDetail?> GetByIdAsync(Guid id);
    Task<MadrasahDetail> CreateAsync(SaveMadrasahRequest req);
    Task<MadrasahDetail?> UpdateAsync(Guid id, SaveMadrasahRequest req);
    Task<bool> DeleteAsync(Guid id);
}
