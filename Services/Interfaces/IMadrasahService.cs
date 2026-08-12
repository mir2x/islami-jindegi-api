using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IMadrasahService
{
    Task<PagedResult<MadrasahListItem>> GetListAsync(int page, int pageSize, string? search, bool? offlineAvailable = null);
    Task<MadrasahDetail?> GetByIdAsync(Guid id);
    Task<IEnumerable<MadrasahDetail>> GetOfflineSyncAsync(DateTime? since);
    Task<List<Guid>> GetOfflineIdsAsync();
    Task<MadrasahDetail> CreateAsync(SaveMadrasahRequest req);
    Task<MadrasahDetail?> UpdateAsync(Guid id, SaveMadrasahRequest req);
    Task<MadrasahListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable);
    Task<bool> DeleteAsync(Guid id);
}
