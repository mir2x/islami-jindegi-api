using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IBayanService
{
    Task<PagedResult<BayanListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, string? sort);
    Task<IEnumerable<BayanAuthorOption>> GetAuthorsAsync(bool published);
    Task<IEnumerable<BayanCategoryOption>> GetCategoriesAsync(bool published);
    Task<BayanDetail?> GetByIdAsync(Guid id);
    Task<(BayanListItem? Item, string? Error)> CreateAsync(SaveBayanRequest req);
    Task<BayanListItem?> UpdateAsync(Guid id, SaveBayanRequest req);
    Task<bool> DeleteAsync(Guid id);
}
