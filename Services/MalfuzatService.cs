using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class MalfuzatService(AppDbContext db, ContentSyncNotifier syncNotifier, PopupAuthorResolver popupAuthor) : IMalfuzatService
{
    /// One random published, text-only malfuzat by the popup author, for the
    /// app's once-a-day home-screen dialog.
    ///
    /// The app used to build this itself: fetch page 1 with pageSize 1 to read
    /// `total`, pick a random page, fetch again. Two round trips per launch per
    /// device, and it carried the author's primary key in the client — which is
    /// what broke, silently, every time a migration reissued that key.
    ///
    /// Rows with no body are excluded: the dialog renders the body, so one
    /// without it is a blank card.
    public async Task<MalfuzatDetail?> GetDailyPopupAsync()
    {
        var authorId = await popupAuthor.ResolveAsync();
        if (authorId is null) return null;

        var item = await db.Malfuzats
            .AsNoTracking()
            .Include(m => m.Author)
            .Include(m => m.Categories)
            .Where(m => m.AuthorId == authorId.Value
                && m.Published
                && !m.HasAudio
                && m.Body != null && m.Body != "")
            .OrderBy(_ => EF.Functions.Random())
            .FirstOrDefaultAsync();

        return item is null ? null : Mappers.ToMalfuzatDetail(item);
    }

    public async Task<PagedResult<MalfuzatListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? hasAudio, bool? offlineAvailable, string? sort, DateOnly? dateFrom = null, DateOnly? dateTo = null)
    {
        var query = db.Malfuzats
            .AsNoTracking()
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
        if (offlineAvailable.HasValue)
            query = query.Where(m => m.IsOfflineAvailable == offlineAvailable.Value);

        // The date filter sends whole days. `dateTo` is inclusive, so it
        // compares against the start of the following day; the column is
        // `timestamp with time zone`, which Npgsql only accepts as UTC.
        //
        // PublishedAt is null for all malfuzat rows — content carried over from the
        // legacy backend kept its creation date and never got a publish date.
        // Filtering on PublishedAt alone would hide all of them, so the date
        // shown to readers, and filtered on here, falls back to CreatedAt.
        if (dateFrom.HasValue)
        {
            var from = DateTime.SpecifyKind(dateFrom.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(m => (m.PublishedAt ?? m.CreatedAt) >= from);
        }
        if (dateTo.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(m => (m.PublishedAt ?? m.CreatedAt) < toExclusive);
        }

        var orderedQuery = sort switch
        {
            "position_desc" => query.OrderByDescending(m => m.Position),
            "position_asc" => query.OrderBy(m => m.Position),
            "title_asc" => query.OrderBy(m => m.Title),
            "title_desc" => query.OrderByDescending(m => m.Title),
            "author_asc" => query.OrderBy(m => m.Author.Name),
            "author_desc" => query.OrderByDescending(m => m.Author.Name),
            "language_asc" => query.OrderBy(m => m.Language),
            "language_desc" => query.OrderByDescending(m => m.Language),
            "audio_asc" => query.OrderBy(m => m.HasAudio).ThenBy(m => m.Position),
            "audio_desc" => query.OrderByDescending(m => m.HasAudio).ThenBy(m => m.Position),
            "published_asc" => query.OrderBy(m => m.Published).ThenBy(m => m.Position),
            "published_desc" => query.OrderByDescending(m => m.Published).ThenBy(m => m.Position),
            _ => query.OrderBy(m => m.Position),
        };

        var total = await query.CountAsync();
        var data = await orderedQuery.ThenBy(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<MalfuzatListItem>(data.Select(Mappers.ToMalfuzatListItem), total, page, pageSize);
    }

    public async Task<IEnumerable<MalfuzatAuthorOption>> GetAuthorsAsync(bool published, string? search = null, int? page = null, int? pageSize = null)
    {
        var query = db.Authors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Name.Contains(search));

        var projected = query
            .Select(a => new { a.Id, a.Name, Count = a.Malfuzats.Count(m => m.Published == published) })
            .Where(a => a.Count > 0)
            .OrderByDescending(a => a.Count)
            .ThenBy(a => a.Name);

        var sliced = page.HasValue && pageSize.HasValue
            ? projected.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value)
            : projected;

        var data = await sliced.ToListAsync();
        return data.Select(a => new MalfuzatAuthorOption(a.Id, a.Name, a.Count));
    }

    public async Task<IEnumerable<MalfuzatCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null)
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
                Count = c.Malfuzats.Count(m => m.Published == published),
                Position = c.Modules
                    .Where(m => m.Module == ContentModules.Malfuzat)
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
        return data.Select(c => new MalfuzatCategoryOption(c.Id, c.Title, c.Count));
    }

    // `hasAudio` scopes previous/next to the Text or Audio tab. Null keeps the
    // corpus-wide sequence used by the All tab and by unscoped callers.
    public async Task<MalfuzatDetail?> GetByIdAsync(Guid id, bool includeUnpublished = false, bool? hasAudio = null)
    {
        var item = await db.Malfuzats
            .AsNoTracking()
            .Include(m => m.Author)
            .Include(m => m.Categories)
            .FirstOrDefaultAsync(m => m.Id == id && (includeUnpublished || m.Published));
        if (item is null) return null;
        var previous = await db.Malfuzats.AsNoTracking()
            .Where(m => m.Published && (hasAudio == null || m.HasAudio == hasAudio) && (m.Position > item.Position || (m.Position == item.Position && m.Id.CompareTo(item.Id) > 0)))
            .OrderBy(m => m.Position).ThenBy(m => m.Id)
            .Select(m => new SiblingRef(m.Id, m.Title, m.Position)).FirstOrDefaultAsync();
        var next = await db.Malfuzats.AsNoTracking()
            .Where(m => m.Published && (hasAudio == null || m.HasAudio == hasAudio) && (m.Position < item.Position || (m.Position == item.Position && m.Id.CompareTo(item.Id) < 0)))
            .OrderByDescending(m => m.Position).ThenByDescending(m => m.Id)
            .Select(m => new SiblingRef(m.Id, m.Title, m.Position)).FirstOrDefaultAsync();
        return Mappers.ToMalfuzatDetail(item) with { Previous = previous, Next = next };
    }

    public async Task<IEnumerable<MalfuzatDetail>> GetOfflineSyncAsync(DateTime? since)
    {
        var items = await db.Malfuzats
            .AsNoTracking()
            .Where(m => m.Published && m.IsOfflineAvailable && (since == null || m.UpdatedAt > since))
            .Include(m => m.Author)
            .Include(m => m.Categories)
            .ToListAsync();
        return items.Select(Mappers.ToMalfuzatDetail);
    }

    public async Task<List<Guid>> GetOfflineIdsAsync()
        => await db.Malfuzats.Where(m => m.Published && m.IsOfflineAvailable).Select(m => m.Id).ToListAsync();

    public async Task<(MalfuzatListItem? Item, string? Error)> CreateAsync(SaveMalfuzatRequest req)
    {
        var author = await db.Authors.FindAsync(req.AuthorId);
        if (author is null) return (null, "Author not found");

        var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();
        var position = req.Position ?? (await db.Malfuzats.MaxAsync(m => (int?)m.Position) ?? 0) + 1;

        var item = new Malfuzat
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Body = req.Body,
            Excerpt = req.Excerpt,
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
        db.Malfuzats.Add(item);
        await db.SaveChangesAsync();
        await db.Entry(item).Reference(m => m.Author).LoadAsync();
        return (Mappers.ToMalfuzatListItem(item), null);
    }

    public async Task<MalfuzatListItem?> UpdateAsync(Guid id, SaveMalfuzatRequest req)
    {
        var item = await db.Malfuzats
            .Include(m => m.Author)
            .Include(m => m.Categories)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (item is null) return null;

        var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();

        item.Title = req.Title;
        item.Body = req.Body;
        item.Excerpt = req.Excerpt;
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
        await db.Entry(item).Reference(m => m.Author).LoadAsync();
        if (item.IsOfflineAvailable) await syncNotifier.NotifyAsync("malfuzats");
        return Mappers.ToMalfuzatListItem(item);
    }

    public async Task<MalfuzatListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable)
    {
        var item = await db.Malfuzats
            .Include(m => m.Author)
            .Include(m => m.Categories)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (item is null) return null;

        item.IsOfflineAvailable = isOfflineAvailable;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await syncNotifier.NotifyAsync("malfuzats");
        return Mappers.ToMalfuzatListItem(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await db.Malfuzats.FindAsync(id);
        if (item is null) return false;
        var wasOfflineAvailable = item.IsOfflineAvailable;
        db.Malfuzats.Remove(item);
        await db.SaveChangesAsync();
        if (wasOfflineAvailable) await syncNotifier.NotifyAsync("malfuzats");
        return true;
    }
}
