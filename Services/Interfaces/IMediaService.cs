using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IMediaService
{
    Task<PagedResult<MediaResponse>> GetListAsync(int page, int pageSize, string? search, string? type);
    Task<MediaResponse?> GetByIdAsync(Guid id);
    Task<MediaResponse> UploadAsync(IFormFile file);
    Task<bool> DeleteAsync(Guid id);
}
