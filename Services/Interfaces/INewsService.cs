using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface INewsService
{
    Task<PagedResult<NewsListItem>> GetListAsync(int page, int pageSize, string? search, bool? published, string? sort);
    Task<NewsDetail?> GetByIdAsync(Guid id);
    Task<NewsDetail> CreateAsync(SaveNewsRequest req);
    Task<NewsDetail?> UpdateAsync(Guid id, SaveNewsRequest req);
    Task<bool> DeleteAsync(Guid id);
}
