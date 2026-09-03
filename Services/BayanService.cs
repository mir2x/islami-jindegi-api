using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class BayanService(AppDbContext db, ContentSyncNotifier syncNotifier) : IBayanService
{
    public async Task<PagedResult<BayanListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? offlineAvailable, string? sort, DateOnly? dateFrom = null, DateOnly? dateTo = null)
    {
        var query = db.Bayans
            .AsNoTracking()
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
        if (offlineAvailable.HasValue)
            query = query.Where(b => b.IsOfflineAvailable == offlineAvailable.Value);

        // The date filter sends whole days. `dateTo` is inclusive, so it
        // compares against the start of the following day; the column is
        // `timestamp with time zone`, which Npgsql only accepts as UTC.
        if (dateFrom.HasValue)
        {
            var from = DateTime.SpecifyKind(dateFrom.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(b => b.PublishedAt >= from);
        }
        if (dateTo.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(b => b.PublishedAt < toExclusive);
        }

        var orderedQuery = sort switch
        {
            // "date" predates the sortable admin columns and is used by the public site — keep it.
            "date" => query.OrderByDescending(b => b.PublishedAt),
            "position_desc" => query.OrderByDescending(b => b.Position),
            "position_asc" => query.OrderBy(b => b.Position),
            "title_asc" => query.OrderBy(b => b.Title),
            "title_desc" => query.OrderByDescending(b => b.Title),
            "author_asc" => query.OrderBy(b => b.Author.Name),
            "author_desc" => query.OrderByDescending(b => b.Author.Name),
            "language_asc" => query.OrderBy(b => b.Language),
            "language_desc" => query.OrderByDescending(b => b.Language),
            "location_asc" => query.OrderBy(b => b.Location),
            "location_desc" => query.OrderByDescending(b => b.Location),
            "date_asc" => query.OrderBy(b => b.PublishedAt),
            "date_desc" => query.OrderByDescending(b => b.PublishedAt),
            "published_asc" => query.OrderBy(b => b.Published).ThenBy(b => b.Position),
            "published_desc" => query.OrderByDescending(b => b.Published).ThenBy(b => b.Position),
            _ => query.OrderBy(b => b.Position),
        };

        var total = await query.CountAsync();
        var data = await orderedQuery.ThenBy(b => b.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<BayanListItem>(data.Select(Mappers.ToBayanListItem), total, page, pageSize);
    }

    public async Task<IEnumerable<BayanAuthorOption>> GetAuthorsAsync(bool published, string? search = null, int? page = null, int? pageSize = null)
    {
        var query = db.Authors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Name.Contains(search));

        // Ordered by the module's own position (author_modules), which was recovered from the
        // legacy per-module author tables. Authors with no position yet sort last.
        var projected = query
            .Select(a => new
            {
                a.Id,
                a.Name,
                Count = a.Bayans.Count(b => b.Published == published),
                Position = a.Modules
                    .Where(m => m.Module == AuthorModules.Bayan)
                    .Select(m => (int?)m.Position)
                    .FirstOrDefault()
            })
            .Where(a => a.Count > 0)
            .OrderBy(a => a.Position == null)
            .ThenBy(a => a.Position)
            .ThenBy(a => a.Name);

        var sliced = page.HasValue && pageSize.HasValue
            ? projected.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value)
            : projected;

        var data = await sliced.ToListAsync();
        return data.Select(a => new BayanAuthorOption(a.Id, a.Name, a.Count));
    }

    public async Task<IEnumerable<BayanCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null)
    {
        var query = db.Categories.Where(c => c.ParentId == null);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search));

        // Ordered by the module's own position (category_modules), which was recovered from
        // the legacy per-module category tables. Categories with no position yet sort last.
        var projected = query
            .Select(c => new
            {
                c.Id,
                c.Title,
                Count = c.Bayans.Count(b => b.Published == published),
                Position = c.Modules
                    .Where(m => m.Module == ContentModules.Bayan)
                    .Select(m => (int?)m.Position)
                    .FirstOrDefault()
            })
            .Where(c => c.Count > 0)
            .OrderBy(c => c.Position == null)
            .ThenBy(c => c.Position)
            .ThenBy(c => c.Title);

        var sliced = page.HasValue && pageSize.HasValue
            ? projected.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value)
            : projected;

        var data = await sliced.ToListAsync();
        return data.Select(c => new BayanCategoryOption(c.Id, c.Title, c.Count));
    }

    public async Task<BayanDetail?> GetByIdAsync(Guid id, bool includeUnpublished = false)
    {
        var item = await db.Bayans
            .AsNoTracking()
            .Include(b => b.Author)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id && (includeUnpublished || b.Published));
        if (item is null) return null;

        // DESC sequence: previous seeks upward, next seeks downward.
        var previous = await db.Bayans.AsNoTracking()
            .Where(b => b.Published && (b.Position > item.Position
                || (b.Position == item.Position && b.Id.CompareTo(item.Id) > 0)))
            .OrderBy(b => b.Position).ThenBy(b => b.Id)
            .Select(b => new SiblingRef(b.Id, b.Title, b.Position))
            .FirstOrDefaultAsync();
        var next = await db.Bayans.AsNoTracking()
            .Where(b => b.Published && (b.Position < item.Position
                || (b.Position == item.Position && b.Id.CompareTo(item.Id) < 0)))
            .OrderByDescending(b => b.Position).ThenByDescending(b => b.Id)
            .Select(b => new SiblingRef(b.Id, b.Title, b.Position))
            .FirstOrDefaultAsync();
        return Mappers.ToBayanDetail(item) with { Previous = previous, Next = next };
    }

    public async Task<IEnumerable<BayanDetail>> GetOfflineSyncAsync(DateTime? since)
    {
        var items = await db.Bayans
            .AsNoTracking()
            .Where(b => b.Published && b.IsOfflineAvailable && (since == null || b.UpdatedAt > since))
            .Include(b => b.Author)
            .Include(b => b.Categories)
            .ToListAsync();
        return items.Select(Mappers.ToBayanDetail);
    }

    public async Task<List<Guid>> GetOfflineIdsAsync()
        => await db.Bayans.Where(b => b.Published && b.IsOfflineAvailable).Select(b => b.Id).ToListAsync();

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
        if (item.IsOfflineAvailable) await syncNotifier.NotifyAsync("bayans");
        return Mappers.ToBayanListItem(item);
    }

    public async Task<BayanListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable)
    {
        var item = await db.Bayans
            .Include(b => b.Author)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (item is null) return null;

        item.IsOfflineAvailable = isOfflineAvailable;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await syncNotifier.NotifyAsync("bayans");
        return Mappers.ToBayanListItem(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await db.Bayans.FindAsync(id);
        if (item is null) return false;
        var wasOfflineAvailable = item.IsOfflineAvailable;
        db.Bayans.Remove(item);
        await db.SaveChangesAsync();
        if (wasOfflineAvailable) await syncNotifier.NotifyAsync("bayans");
        return true;
    }
}
