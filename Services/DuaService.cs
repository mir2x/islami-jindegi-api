using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class DuaService(AppDbContext db) : IDuaService
{
    public async Task<PagedResult<DuaListItem>> GetListAsync(int page, int pageSize, string? search, Guid? categoryId, bool? published, bool? hasAudio, string? sort)
    {
        var query = db.Duas
            .Include(d => d.Categories)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.Title.Contains(search));
        if (categoryId.HasValue)
            query = query.Where(d => d.Categories.Any(c => c.Id == categoryId.Value));
        if (published.HasValue)
            query = query.Where(d => d.Published == published.Value);
        if (hasAudio.HasValue)
            query = hasAudio.Value
                ? query.Where(d => d.AudioUrl != null)
                : query.Where(d => d.AudioUrl == null);

        query = sort == "position_desc"
            ? query.OrderByDescending(d => d.Position)
            : query.OrderBy(d => d.Position);

        var total = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<DuaListItem>(data.Select(Mappers.ToDuaListItem), total, page, pageSize);
    }

    public async Task<IEnumerable<DuaCategoryOption>> GetCategoriesAsync(bool published)
    {
        var data = await db.Categories
            .Where(c => c.ParentId == null)
            .Select(c => new { c.Id, c.Title, Count = c.Duas.Count(d => d.Published == published) })
            .Where(c => c.Count > 0)
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Title)
            .ToListAsync();
        return data.Select(c => new DuaCategoryOption(c.Id, c.Title, c.Count));
    }

    public async Task<DuaDetail?> GetByIdAsync(Guid id)
    {
        var item = await db.Duas
            .Include(d => d.Categories)
            .FirstOrDefaultAsync(d => d.Id == id);
        return item is null ? null : Mappers.ToDuaDetail(item);
    }

    public async Task<DuaListItem> CreateAsync(SaveDuaRequest req)
    {
        var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();
        var position = req.Position ?? (await db.Duas.MaxAsync(d => (int?)d.Position) ?? 0) + 1;

        var item = new Dua
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Body = req.Body,
            Excerpt = req.Excerpt,
            Language = req.Language,
            AudioUrl = req.AudioUrl,
            DocumentUrl = req.DocumentUrl,
            Published = req.Published,
            Position = position,
            Categories = categories,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Duas.Add(item);
        await db.SaveChangesAsync();
        return Mappers.ToDuaListItem(item);
    }

    public async Task<DuaListItem?> UpdateAsync(Guid id, SaveDuaRequest req)
    {
        var item = await db.Duas
            .Include(d => d.Categories)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (item is null) return null;

        var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();

        item.Title = req.Title;
        item.Body = req.Body;
        item.Excerpt = req.Excerpt;
        item.Language = req.Language;
        item.AudioUrl = req.AudioUrl;
        item.DocumentUrl = req.DocumentUrl;
        item.Published = req.Published;
        if (req.Position.HasValue) item.Position = req.Position.Value;
        item.Categories = categories;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Mappers.ToDuaListItem(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await db.Duas.FindAsync(id);
        if (item is null) return false;
        db.Duas.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }
}
