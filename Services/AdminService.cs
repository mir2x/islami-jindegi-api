using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class AdminService(AppDbContext db) : IAdminService
{
    public async Task<List<AdminResponse>> GetListAsync() =>
        await db.Admins
            .OrderBy(a => a.CreatedAt)
            .Select(a => Mappers.ToAdminResponse(a))
            .ToListAsync();

    public async Task<Admin?> GetByEmailAsync(string email) =>
        await db.Admins.FirstOrDefaultAsync(a => a.Email == email.Trim().ToLower());

    public async Task<AdminResponse> CreateAsync(CreateAdminRequest req)
    {
        var admin = new Admin
        {
            Id = Guid.NewGuid(),
            Email = req.Email.Trim().ToLower(),
            DisplayName = req.DisplayName,
            CreatedAt = DateTime.UtcNow
        };
        db.Admins.Add(admin);
        await db.SaveChangesAsync();
        return Mappers.ToAdminResponse(admin);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var admin = await db.Admins.FindAsync(id);
        if (admin is null) return false;
        db.Admins.Remove(admin);
        await db.SaveChangesAsync();
        return true;
    }
}
