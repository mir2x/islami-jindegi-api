using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IMasailService
{
    Task<PagedResult<MasailListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? hasAudio);
    Task<IEnumerable<MasailAuthorOption>> GetAuthorsAsync(bool published);
    Task<IEnumerable<MasailCategoryOption>> GetCategoriesAsync(bool published);
    Task<MasailDetail?> GetByIdAsync(Guid id);
    Task<MasailListItem> CreateAsync(SaveMasailRequest req);
    Task<MasailListItem?> UpdateAsync(Guid id, SaveMasailRequest req);
    Task<bool> DeleteAsync(Guid id);
}
