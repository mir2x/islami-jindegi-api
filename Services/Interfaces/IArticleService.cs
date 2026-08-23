using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IArticleService
{
    Task<PagedResult<ArticleListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? offlineAvailable, string? sort, DateOnly? dateFrom = null, DateOnly? dateTo = null);
    Task<IEnumerable<ArticleAuthorOption>> GetAuthorsAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<IEnumerable<ArticleCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<ArticleDetail?> GetByIdAsync(Guid id, bool includeUnpublished = false);
    Task<IEnumerable<ArticleDetail>> GetOfflineSyncAsync(DateTime? since);
    Task<List<Guid>> GetOfflineIdsAsync();
    Task<ArticleListItem> CreateAsync(SaveArticleRequest req);
    Task<ArticleListItem?> UpdateAsync(Guid id, SaveArticleRequest req);
    Task<ArticleListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable);
    Task<bool> DeleteAsync(Guid id);
}
