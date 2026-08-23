using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class ArticleService(AppDbContext db, ContentSyncNotifier syncNotifier) : IArticleService
{
    public async Task<PagedResult<ArticleListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? offlineAvailable, string? sort, DateOnly? dateFrom = null, DateOnly? dateTo = null)
    {
        var query = db.Articles
            .AsNoTracking()
            .Include(a => a.Author)
            .Include(a => a.Categories)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Title.Contains(search));
        if (authorId.HasValue)
            query = query.Where(a => a.AuthorId == authorId.Value);
        if (categoryId.HasValue)
            query = query.Where(a => a.Categories.Any(c => c.Id == categoryId.Value));
        if (published.HasValue)
            query = query.Where(a => a.Published == published.Value);
        if (offlineAvailable.HasValue)
            query = query.Where(a => a.IsOfflineAvailable == offlineAvailable.Value);

        // The date filter sends whole days. `dateTo` is inclusive, so it
        // compares against the start of the following day; the column is
        // `timestamp with time zone`, which Npgsql only accepts as UTC.
        //
        // PublishedAt is null for about 80% of article rows — content carried over from the
        // legacy backend kept its creation date and never got a publish date.
        // Filtering on PublishedAt alone would hide all of them, so the date
        // shown to readers, and filtered on here, falls back to CreatedAt.
        if (dateFrom.HasValue)
        {
            var from = DateTime.SpecifyKind(dateFrom.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(a => (a.PublishedAt ?? a.CreatedAt) >= from);
        }
        if (dateTo.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(a => (a.PublishedAt ?? a.CreatedAt) < toExclusive);
        }

        query = sort switch
        {
            "position_desc" => query.OrderByDescending(a => a.Position),
            "position_asc" => query.OrderBy(a => a.Position),
            "title_asc" => query.OrderBy(a => a.Title),
            "title_desc" => query.OrderByDescending(a => a.Title),
            "author_asc" => query.OrderBy(a => a.Author!.Name),
            "author_desc" => query.OrderByDescending(a => a.Author!.Name),
            "language_asc" => query.OrderBy(a => a.Language),
            "language_desc" => query.OrderByDescending(a => a.Language),
            "published_asc" => query.OrderBy(a => a.Published).ThenBy(a => a.Position),
            "published_desc" => query.OrderByDescending(a => a.Published).ThenBy(a => a.Position),
            _ => query.OrderBy(a => a.Position),
        };

        var total = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ArticleListItem>(data.Select(Mappers.ToArticleListItem), total, page, pageSize);
    }

    public async Task<IEnumerable<ArticleAuthorOption>> GetAuthorsAsync(bool published, string? search = null, int? page = null, int? pageSize = null)
    {
        var query = db.Authors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Name.Contains(search));

        var projected = query
            .Select(a => new { a.Id, a.Name, Count = a.Articles.Count(x => x.Published == published) })
            .Where(a => a.Count > 0)
            .OrderByDescending(a => a.Count)
            .ThenBy(a => a.Name);

        var sliced = page.HasValue && pageSize.HasValue
            ? projected.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value)
            : projected;

        var data = await sliced.ToListAsync();
        return data.Select(a => new ArticleAuthorOption(a.Id, a.Name, a.Count));
    }

    public async Task<IEnumerable<ArticleCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null)
    {
        var query = db.Categories.Where(c => c.ParentId == null);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search));

        var projected = query
            .Select(c => new { c.Id, c.Title, Count = c.Articles.Count(x => x.Published == published) })
            .Where(c => c.Count > 0)
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Title);

        var sliced = page.HasValue && pageSize.HasValue
            ? projected.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value)
            : projected;

        var data = await sliced.ToListAsync();
        return data.Select(c => new ArticleCategoryOption(c.Id, c.Title, c.Count));
    }

    public async Task<ArticleDetail?> GetByIdAsync(Guid id)
    {
        var item = await db.Articles
            .AsNoTracking()
            .Include(a => a.Author)
            .Include(a => a.Categories)
            .FirstOrDefaultAsync(a => a.Id == id);
        return item is null ? null : Mappers.ToArticleDetail(item);
    }

    public async Task<IEnumerable<ArticleDetail>> GetOfflineSyncAsync(DateTime? since)
    {
        var items = await db.Articles
            .AsNoTracking()
            .Where(a => a.IsOfflineAvailable && (since == null || a.UpdatedAt > since))
            .Include(a => a.Author)
            .Include(a => a.Categories)
            .ToListAsync();
        return items.Select(Mappers.ToArticleDetail);
    }

    public async Task<List<Guid>> GetOfflineIdsAsync()
        => await db.Articles.Where(a => a.IsOfflineAvailable).Select(a => a.Id).ToListAsync();

    public async Task<ArticleListItem> CreateAsync(SaveArticleRequest req)
    {
        var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();
        var position = req.Position ?? (await db.Articles.MaxAsync(a => (int?)a.Position) ?? 0) + 1;

        var item = new Article
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Body = req.Body,
            Excerpt = req.Excerpt,
            Language = req.Language,
            DocumentUrl = req.DocumentUrl,
            Published = req.Published,
            PublishedAt = req.PublishedAt,
            Position = position,
            AuthorId = req.AuthorId,
            Categories = categories,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Articles.Add(item);
        await db.SaveChangesAsync();
        if (item.AuthorId.HasValue)
            await db.Entry(item).Reference(a => a.Author).LoadAsync();
        return Mappers.ToArticleListItem(item);
    }

    public async Task<ArticleListItem?> UpdateAsync(Guid id, SaveArticleRequest req)
    {
        var item = await db.Articles
            .Include(a => a.Author)
            .Include(a => a.Categories)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (item is null) return null;

        var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();

        item.Title = req.Title;
        item.Body = req.Body;
        item.Excerpt = req.Excerpt;
        item.Language = req.Language;
        item.DocumentUrl = req.DocumentUrl;
        item.Published = req.Published;
        item.PublishedAt = req.PublishedAt;
        if (req.Position.HasValue) item.Position = req.Position.Value;
        item.AuthorId = req.AuthorId;
        item.Categories = categories;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        if (item.AuthorId.HasValue)
            await db.Entry(item).Reference(a => a.Author).LoadAsync();
        if (item.IsOfflineAvailable) await syncNotifier.NotifyAsync("articles");
        return Mappers.ToArticleListItem(item);
    }

    public async Task<ArticleListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable)
    {
        var item = await db.Articles
            .Include(a => a.Author)
            .Include(a => a.Categories)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (item is null) return null;

        item.IsOfflineAvailable = isOfflineAvailable;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await syncNotifier.NotifyAsync("articles");
        return Mappers.ToArticleListItem(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await db.Articles.FindAsync(id);
        if (item is null) return false;
        var wasOfflineAvailable = item.IsOfflineAvailable;
        db.Articles.Remove(item);
        await db.SaveChangesAsync();
        if (wasOfflineAvailable) await syncNotifier.NotifyAsync("articles");
        return true;
    }
}
