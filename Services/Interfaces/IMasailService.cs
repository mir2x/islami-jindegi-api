using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IMasailService
{
    Task<PagedResult<MasailListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? hasAudio, bool? offlineAvailable, string? sort);
    Task<IEnumerable<MasailAuthorOption>> GetAuthorsAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<IEnumerable<MasailCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<MasailDetail?> GetByIdAsync(Guid id);
    Task<IEnumerable<MasailDetail>> GetOfflineSyncAsync(DateTime? since);
    Task<List<Guid>> GetOfflineIdsAsync();
    Task<MasailListItem> CreateAsync(SaveMasailRequest req);
    Task<MasailListItem?> UpdateAsync(Guid id, SaveMasailRequest req);
    Task<MasailListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable);
    Task<bool> DeleteAsync(Guid id);
}
