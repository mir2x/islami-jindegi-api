using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class MasailService(AppDbContext db) : IMasailService
{
    public async Task<PagedResult<MasailListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? hasAudio, string? sort)
    {
        var query = db.Masails
            .Include(m => m.Author)
            .Include(m => m.Categories)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Title.Contains(search));
        if (authorId.HasValue)
            query = query.Where(m => m.AuthorId == authorId.Value);
        if (categoryId.HasValue)
            query = query.Where(m => m.Categories.Any(c => c.Id == categoryId.Value));
        if (published.HasValue)
            query = query.Where(m => m.Published == published.Value);
        if (hasAudio.HasValue)
            query = query.Where(m => m.HasAudio == hasAudio.Value);

        query = sort == "position_desc"
            ? query.OrderByDescending(m => m.Position)
            : query.OrderBy(m => m.Position);

        var total = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<MasailListItem>(data.Select(Mappers.ToMasailListItem), total, page, pageSize);
    }

    public async Task<IEnumerable<MasailAuthorOption>> GetAuthorsAsync(bool published, string? search = null, int? page = null, int? pageSize = null)
    {
        var query = db.Authors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Name.Contains(search));

        var projected = query
            .Select(a => new { a.Id, a.Name, Count = a.Masails.Count(m => m.Published == published) })
            .Where(a => a.Count > 0)
            .OrderByDescending(a => a.Count)
            .ThenBy(a => a.Name);

        var sliced = page.HasValue && pageSize.HasValue
            ? projected.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value)
            : projected;

        var data = await sliced.ToListAsync();
        return data.Select(a => new MasailAuthorOption(a.Id, a.Name, a.Count));
    }

    public async Task<IEnumerable<MasailCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null)
    {
        var query = db.Categories.Where(c => c.ParentId == null);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search));

        var projected = query
            .Select(c => new { c.Id, c.Title, Count = c.Masails.Count(m => m.Published == published) })
            .Where(c => c.Count > 0)
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Title);

        var sliced = page.HasValue && pageSize.HasValue
            ? projected.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value)
            : projected;

        var data = await sliced.ToListAsync();
        return data.Select(c => new MasailCategoryOption(c.Id, c.Title, c.Count));
    }

    public async Task<MasailDetail?> GetByIdAsync(Guid id)
    {
        var item = await db.Masails
            .Include(m => m.Author)
            .Include(m => m.Categories)
            .FirstOrDefaultAsync(m => m.Id == id);
        return item is null ? null : Mappers.ToMasailDetail(item);
    }

    public async Task<MasailListItem> CreateAsync(SaveMasailRequest req)
    {
        var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();
        var position = req.Position ?? (await db.Masails.MaxAsync(m => (int?)m.Position) ?? 0) + 1;

        var item = new Masail
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Question = req.Question,
            Answer = req.Answer,
            Language = req.Language,
            HasAudio = req.HasAudio,
            AudioUrl = req.AudioUrl,
            DocumentUrl = req.DocumentUrl,
            Published = req.Published,
            PublishedAt = req.PublishedAt,
            Position = position,
            AuthorId = req.AuthorId,
            Categories = categories,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Masails.Add(item);
        await db.SaveChangesAsync();
        if (item.AuthorId.HasValue)
            await db.Entry(item).Reference(m => m.Author).LoadAsync();
        return Mappers.ToMasailListItem(item);
    }

    public async Task<MasailListItem?> UpdateAsync(Guid id, SaveMasailRequest req)
    {
        var item = await db.Masails
            .Include(m => m.Author)
            .Include(m => m.Categories)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (item is null) return null;

        var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();

        item.Title = req.Title;
        item.Question = req.Question;
        item.Answer = req.Answer;
        item.Language = req.Language;
        item.HasAudio = req.HasAudio;
        item.AudioUrl = req.AudioUrl;
        item.DocumentUrl = req.DocumentUrl;
        item.Published = req.Published;
        item.PublishedAt = req.PublishedAt;
        if (req.Position.HasValue) item.Position = req.Position.Value;
        item.AuthorId = req.AuthorId;
        item.Categories = categories;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        if (item.AuthorId.HasValue)
            await db.Entry(item).Reference(m => m.Author).LoadAsync();
        return Mappers.ToMasailListItem(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await db.Masails.FindAsync(id);
        if (item is null) return false;
        db.Masails.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }
}
