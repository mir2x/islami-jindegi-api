using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IAuthorService
{
    Task<PagedResult<AuthorResponse>> GetListAsync(int page, int pageSize, string? search);
    Task<AuthorResponse?> GetByIdAsync(Guid id);
    Task<AuthorResponse> CreateAsync(CreateAuthorRequest req);
    Task<AuthorResponse?> UpdateAsync(Guid id, UpdateAuthorRequest req);
    Task<bool> DeleteAsync(Guid id);
}
