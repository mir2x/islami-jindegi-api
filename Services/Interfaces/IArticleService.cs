using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IArticleService
{
    Task<PagedResult<ArticleListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, string? sort);
    Task<IEnumerable<ArticleAuthorOption>> GetAuthorsAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<IEnumerable<ArticleCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<ArticleDetail?> GetByIdAsync(Guid id);
    Task<ArticleListItem> CreateAsync(SaveArticleRequest req);
    Task<ArticleListItem?> UpdateAsync(Guid id, SaveArticleRequest req);
    Task<bool> DeleteAsync(Guid id);
}
