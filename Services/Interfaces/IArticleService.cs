using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IArticleService
{
    Task<PagedResult<ArticleListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published);
    Task<IEnumerable<ArticleAuthorOption>> GetAuthorsAsync(bool published);
    Task<IEnumerable<ArticleCategoryOption>> GetCategoriesAsync(bool published);
    Task<ArticleDetail?> GetByIdAsync(Guid id);
    Task<ArticleListItem> CreateAsync(SaveArticleRequest req);
    Task<ArticleListItem?> UpdateAsync(Guid id, SaveArticleRequest req);
    Task<bool> DeleteAsync(Guid id);
}
