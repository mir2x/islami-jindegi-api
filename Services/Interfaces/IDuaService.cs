using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IDuaService
{
    Task<PagedResult<DuaListItem>> GetListAsync(int page, int pageSize, string? search, Guid? categoryId, bool? published, bool? hasAudio, string? sort);
    Task<IEnumerable<DuaCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null);
    Task<DuaDetail?> GetByIdAsync(Guid id);
    Task<DuaListItem> CreateAsync(SaveDuaRequest req);
    Task<DuaListItem?> UpdateAsync(Guid id, SaveDuaRequest req);
    Task<bool> DeleteAsync(Guid id);
}
