using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IMalfuzatService
{
    Task<PagedResult<MalfuzatListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? hasAudio, bool? offlineAvailable, string? sort, DateOnly? dateFrom = null, DateOnly? dateTo = null);
    Task<IEnumerable<MalfuzatAuthorOption>> GetAuthorsAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<IEnumerable<MalfuzatCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<MalfuzatDetail?> GetByIdAsync(Guid id);
    Task<IEnumerable<MalfuzatDetail>> GetOfflineSyncAsync(DateTime? since);
    Task<List<Guid>> GetOfflineIdsAsync();
    Task<(MalfuzatListItem? Item, string? Error)> CreateAsync(SaveMalfuzatRequest req);
    Task<MalfuzatListItem?> UpdateAsync(Guid id, SaveMalfuzatRequest req);
    Task<MalfuzatListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable);
    Task<bool> DeleteAsync(Guid id);
}
