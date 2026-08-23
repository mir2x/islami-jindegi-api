using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IDuaService
{
    Task<PagedResult<DuaListItem>> GetListAsync(int page, int pageSize, string? search, Guid? categoryId, bool? published, bool? hasAudio, bool? offlineAvailable, string? sort);
    Task<IEnumerable<DuaCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<DuaDetail?> GetByIdAsync(Guid id, bool includeUnpublished = false);
    Task<IEnumerable<DuaDetail>> GetOfflineSyncAsync(DateTime? since);
    Task<List<Guid>> GetOfflineIdsAsync();
    Task<DuaListItem> CreateAsync(SaveDuaRequest req);
    Task<DuaListItem?> UpdateAsync(Guid id, SaveDuaRequest req);
    Task<DuaListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable);
    Task<bool> DeleteAsync(Guid id);
}
