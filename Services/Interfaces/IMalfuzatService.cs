using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IMalfuzatService
{
    Task<PagedResult<MalfuzatListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? hasAudio, string? sort);
    Task<IEnumerable<MalfuzatAuthorOption>> GetAuthorsAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<IEnumerable<MalfuzatCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<MalfuzatDetail?> GetByIdAsync(Guid id);
    Task<(MalfuzatListItem? Item, string? Error)> CreateAsync(SaveMalfuzatRequest req);
    Task<MalfuzatListItem?> UpdateAsync(Guid id, SaveMalfuzatRequest req);
    Task<bool> DeleteAsync(Guid id);
}
