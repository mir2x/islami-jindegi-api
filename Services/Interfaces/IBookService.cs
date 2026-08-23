using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IBookService
{
    Task<PagedResult<BookListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? offlineAvailable, string? sort);
    Task<IEnumerable<BookAuthorOption>> GetAuthorsAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<IEnumerable<BookCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<BookDetail?> GetByIdAsync(Guid id, bool includeUnpublished = false);
    Task<IEnumerable<BookDetail>> GetOfflineSyncAsync(DateTime? since);
    Task<List<Guid>> GetOfflineIdsAsync();
    Task<BookListItem> CreateAsync(SaveBookRequest req);
    Task<BookListItem?> UpdateAsync(Guid id, SaveBookRequest req);
    Task<BookListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable);
    Task<bool> DeleteAsync(Guid id);
}
