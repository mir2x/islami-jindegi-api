using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponse>> GetAllAsync();
    Task<PagedResult<CategoryResponse>> GetPagedAsync(int page, int pageSize, string? search, string? sort);
    Task<CategoryResponse?> GetByIdAsync(Guid id);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest req);
    Task<CategoryResponse?> UpdateAsync(Guid id, UpdateCategoryRequest req);
    Task<bool> DeleteAsync(Guid id);
}
