using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IAuthorService
{
    Task<IEnumerable<AuthorResponse>> GetAllAsync();
    Task<PagedResult<AuthorResponse>> GetListAsync(int page, int pageSize, string? search, string? sort = null);
    Task<AuthorResponse?> GetByIdAsync(Guid id);
    Task<AuthorWriteResult> CreateAsync(CreateAuthorRequest req);
    Task<AuthorWriteResult> UpdateAsync(Guid id, UpdateAuthorRequest req);
    Task<AuthorWriteResult> DeleteAsync(Guid id);
    Task<IEnumerable<AuthorUsage>> GetUsageAsync(Guid id);
    Task<AuthorWriteResult> MergeAsync(Guid sourceId, Guid targetId);
    Task<AuthorWriteResult> ReorderAsync(ReorderAuthorsRequest req);
}
