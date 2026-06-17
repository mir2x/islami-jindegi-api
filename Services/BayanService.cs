using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class BayanService(AppDbContext db) : IBayanService
{
    public async Task<PagedResult<BayanListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, string? sort)
    {
        var query = db.Bayans
            .Include(b => b.Author)
            .Include(b => b.Categories)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(b => b.Title.Contains(search));
        if (authorId.HasValue)
            query = query.Where(b => b.AuthorId == authorId.Value);
        if (categoryId.HasValue)
            query = query.Where(b => b.Categories.Any(c => c.Id == categoryId.Value));
        if (published.HasValue)
            query = query.Where(b => b.Published == published.Value);

        query = sort == "date"
            ? query.OrderByDescending(b => b.PublishedAt)
            : query.OrderBy(b => b.Position);

        var total = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<BayanListItem>(data.Select(Mappers.ToBayanListItem), total, page, pageSize);
    }

    public async Task<IEnumerable<BayanAuthorOption>> GetAuthorsAsync(bool published)
    {
        var data = await db.Authors
            .Select(a => new { a.Id, a.Name, Count = a.Bayans.Count(b => b.Published == published) })
            .Where(a => a.Count > 0)
            .OrderByDescending(a => a.Count)
            .ThenBy(a => a.Name)
            .ToListAsync();
        return data.Select(a => new BayanAuthorOption(a.Id, a.Name, a.Count));
    }

    public async Task<IEnumerable<BayanCategoryOption>> GetCategoriesAsync(bool published)
    {
        var data = await db.Categories
            .Where(c => c.ParentId == null)
            .Select(c => new { c.Id, c.Title, Count = c.Bayans.Count(b => b.Published == published) })
            .Where(c => c.Count > 0)
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Title)
            .ToListAsync();
        return data.Select(c => new BayanCategoryOption(c.Id, c.Title, c.Count));
    }

    public async Task<BayanDetail?> GetByIdAsync(Guid id)
    {
        var item = await db.Bayans
            .Include(b => b.Author)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id);
        return item is null ? null : Mappers.ToBayanDetail(item);
    }

    public async Task<(BayanListItem? Item, string? Error)> CreateAsync(SaveBayanRequest req)
    {
        var author = await db.Authors.FindAsync(req.AuthorId);
        if (author is null) return (null, "Author not found");

        var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();
        var position = req.Position ?? (await db.Bayans.MaxAsync(b => (int?)b.Position) ?? 0) + 1;

        var item = new Bayan
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Excerpt = req.Excerpt,
            Language = req.Language,
            Location = req.Location,
            AudioUrl = req.AudioUrl,
            Published = req.Published,
            PublishedAt = req.PublishedAt,
            Position = position,
            AuthorId = req.AuthorId,
            Categories = categories,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Bayans.Add(item);
        await db.SaveChangesAsync();
        await db.Entry(item).Reference(b => b.Author).LoadAsync();
        return (Mappers.ToBayanListItem(item), null);
    }

    public async Task<BayanListItem?> UpdateAsync(Guid id, SaveBayanRequest req)
    {
        var item = await db.Bayans
            .Include(b => b.Author)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (item is null) return null;

        var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();

        item.Title = req.Title;
        item.Excerpt = req.Excerpt;
        item.Language = req.Language;
        item.Location = req.Location;
        item.AudioUrl = req.AudioUrl;
        item.Published = req.Published;
        item.PublishedAt = req.PublishedAt;
        if (req.Position.HasValue) item.Position = req.Position.Value;
        item.AuthorId = req.AuthorId;
        item.Categories = categories;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await db.Entry(item).Reference(b => b.Author).LoadAsync();
        return Mappers.ToBayanListItem(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await db.Bayans.FindAsync(id);
        if (item is null) return false;
        db.Bayans.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }
}
