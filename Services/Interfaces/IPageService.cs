using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IPageService
{
    Task<PagedResult<PageListItem>> GetListAsync(int page, int pageSize, string? search, bool? offlineAvailable = null);
    Task<PageDetail?> GetByIdAsync(Guid id);
    Task<PageDetail?> GetBySlugAsync(string slug);
    Task<IEnumerable<PageDetail>> GetOfflineSyncAsync(DateTime? since);
    Task<List<Guid>> GetOfflineIdsAsync();
    Task<(PageDetail? Item, string? Error)> CreateAsync(SavePageRequest req);
    Task<(PageDetail? Item, string? Error)> UpdateAsync(Guid id, SavePageRequest req);
    Task<PageListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable);
    Task<bool> DeleteAsync(Guid id);
}
