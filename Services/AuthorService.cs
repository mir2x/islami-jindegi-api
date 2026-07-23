using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class AuthorService(AppDbContext db) : IAuthorService
{
    public async Task<PagedResult<AuthorResponse>> GetListAsync(int page, int pageSize, string? search, string? sort = null)
    {
        var query = db.Authors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Name.Contains(search));

        query = sort switch
        {
            "position_desc" => query.OrderByDescending(a => a.Position),
            "position_asc" => query.OrderBy(a => a.Position),
            "name_asc" => query.OrderBy(a => a.Name),
            "name_desc" => query.OrderByDescending(a => a.Name),
            _ => query.OrderBy(a => a.Position),
        };

        var total = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => Mappers.ToAuthorResponse(a))
            .ToListAsync();

        return new PagedResult<AuthorResponse>(data, total, page, pageSize);
    }

    public async Task<AuthorResponse?> GetByIdAsync(Guid id)
    {
        var author = await db.Authors.FindAsync(id);
        return author is null ? null : Mappers.ToAuthorResponse(author);
    }

    public async Task<AuthorResponse> CreateAsync(CreateAuthorRequest req)
    {
        var position = req.Position ?? (await db.Authors.MaxAsync(a => (int?)a.Position) ?? 0) + 1;
        var author = new Author
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Info = req.Info,
            Position = position,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Authors.Add(author);
        await db.SaveChangesAsync();
        return Mappers.ToAuthorResponse(author);
    }

    public async Task<AuthorResponse?> UpdateAsync(Guid id, UpdateAuthorRequest req)
    {
        var author = await db.Authors.FindAsync(id);
        if (author is null) return null;

        author.Name = req.Name;
        author.Info = req.Info;
        if (req.Position.HasValue) author.Position = req.Position.Value;
        author.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Mappers.ToAuthorResponse(author);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var author = await db.Authors.FindAsync(id);
        if (author is null) return false;
        db.Authors.Remove(author);
        await db.SaveChangesAsync();
        return true;
    }
}
