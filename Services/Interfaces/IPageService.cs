using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IPageService
{
    Task<PagedResult<PageListItem>> GetListAsync(int page, int pageSize, string? search);
    Task<PageDetail?> GetByIdAsync(Guid id);
    Task<PageDetail?> GetBySlugAsync(string slug);
    Task<(PageDetail? Item, string? Error)> CreateAsync(SavePageRequest req);
    Task<(PageDetail? Item, string? Error)> UpdateAsync(Guid id, SavePageRequest req);
    Task<bool> DeleteAsync(Guid id);
}
