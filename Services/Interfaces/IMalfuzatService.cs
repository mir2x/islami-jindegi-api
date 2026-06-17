using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IMalfuzatService
{
    Task<PagedResult<MalfuzatListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? hasAudio);
    Task<IEnumerable<MalfuzatAuthorOption>> GetAuthorsAsync(bool published);
    Task<IEnumerable<MalfuzatCategoryOption>> GetCategoriesAsync(bool published);
    Task<MalfuzatDetail?> GetByIdAsync(Guid id);
    Task<(MalfuzatListItem? Item, string? Error)> CreateAsync(SaveMalfuzatRequest req);
    Task<MalfuzatListItem?> UpdateAsync(Guid id, SaveMalfuzatRequest req);
    Task<bool> DeleteAsync(Guid id);
}
