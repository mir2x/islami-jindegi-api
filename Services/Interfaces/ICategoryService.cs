using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponse>> GetAllAsync();
    Task<PagedResult<CategoryResponse>> GetPagedAsync(int page, int pageSize, string? search, string? sort);
    Task<CategoryResponse?> GetByIdAsync(Guid id);
    Task<CategoryWriteResult> CreateAsync(CreateCategoryRequest req);
    Task<CategoryWriteResult> UpdateAsync(Guid id, UpdateCategoryRequest req);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<CategoryUsage>> GetUsageAsync(Guid id);
    Task<CategoryWriteResult> MergeAsync(Guid sourceId, Guid targetId);
    Task<CategoryWriteResult> ReorderAsync(ReorderCategoriesRequest req);
}
