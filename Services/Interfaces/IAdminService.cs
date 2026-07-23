using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;

namespace IslamiJindegiApi.Services;

public interface IAdminService
{
    Task<List<AdminResponse>> GetListAsync();
    Task<Admin?> GetByEmailAsync(string email);
    Task<AdminResponse> CreateAsync(CreateAdminRequest req);
    Task<bool> DeleteAsync(Guid id);
}
