using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IBayanService
{
    Task<PagedResult<BayanListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? offlineAvailable, string? sort);
    Task<IEnumerable<BayanAuthorOption>> GetAuthorsAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<IEnumerable<BayanCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<BayanDetail?> GetByIdAsync(Guid id);
    Task<IEnumerable<BayanDetail>> GetOfflineSyncAsync(DateTime? since);
    Task<List<Guid>> GetOfflineIdsAsync();
    Task<(BayanListItem? Item, string? Error)> CreateAsync(SaveBayanRequest req);
    Task<BayanListItem?> UpdateAsync(Guid id, SaveBayanRequest req);
    Task<BayanListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable);
    Task<bool> DeleteAsync(Guid id);
}
