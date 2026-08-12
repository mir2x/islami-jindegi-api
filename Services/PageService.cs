using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class PageService(AppDbContext db, ContentSyncNotifier syncNotifier) : IPageService
{
    public async Task<PagedResult<PageListItem>> GetListAsync(int page, int pageSize, string? search, bool? offlineAvailable = null)
    {
        var query = db.Pages.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Title.Contains(search) || p.Slug.Contains(search));
        if (offlineAvailable.HasValue)
            query = query.Where(p => p.IsOfflineAvailable == offlineAvailable.Value);

        var total = await query.CountAsync();
        var data = await query
            .OrderBy(p => p.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => Mappers.ToPageListItem(p))
            .ToListAsync();

        return new PagedResult<PageListItem>(data, total, page, pageSize);
    }

    public async Task<PageDetail?> GetByIdAsync(Guid id)
    {
        var item = await db.Pages.FindAsync(id);
        return item is null ? null : Mappers.ToPageDetail(item);
    }

    public async Task<PageDetail?> GetBySlugAsync(string slug)
    {
        var item = await db.Pages.FirstOrDefaultAsync(p => p.Slug == slug);
        return item is null ? null : Mappers.ToPageDetail(item);
    }

    public async Task<IEnumerable<PageDetail>> GetOfflineSyncAsync(DateTime? since)
    {
        var items = await db.Pages
            .Where(p => p.IsOfflineAvailable && (since == null || p.UpdatedAt > since))
            .ToListAsync();
        return items.Select(Mappers.ToPageDetail);
    }

    public async Task<List<Guid>> GetOfflineIdsAsync()
        => await db.Pages.Where(p => p.IsOfflineAvailable).Select(p => p.Id).ToListAsync();

    public async Task<(PageDetail? Item, string? Error)> CreateAsync(SavePageRequest req)
    {
        var slugTaken = await db.Pages.AnyAsync(p => p.Slug == req.Slug);
        if (slugTaken) return (null, "A page with this slug already exists");

        var item = new Page
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Slug = req.Slug,
            Body = req.Body,
            ImageUrl = req.ImageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Pages.Add(item);
        await db.SaveChangesAsync();
        return (Mappers.ToPageDetail(item), null);
    }

    public async Task<(PageDetail? Item, string? Error)> UpdateAsync(Guid id, SavePageRequest req)
    {
        var item = await db.Pages.FindAsync(id);
        if (item is null) return (null, null);

        var slugTaken = await db.Pages.AnyAsync(p => p.Slug == req.Slug && p.Id != id);
        if (slugTaken) return (null, "A page with this slug already exists");

        item.Title = req.Title;
        item.Slug = req.Slug;
        item.Body = req.Body;
        item.ImageUrl = req.ImageUrl;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        // Pages ride along with the "masails" feature on the client (see
        // MasailSyncService, which syncs both /masail/offline-sync and
        // /pages/offline-sync) — there's no standalone "pages" sync dispatch.
        if (item.IsOfflineAvailable) await syncNotifier.NotifyAsync("masails");
        return (Mappers.ToPageDetail(item), null);
    }

    public async Task<PageListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable)
    {
        var item = await db.Pages.FindAsync(id);
        if (item is null) return null;

        item.IsOfflineAvailable = isOfflineAvailable;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await syncNotifier.NotifyAsync("masails");
        return Mappers.ToPageListItem(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await db.Pages.FindAsync(id);
        if (item is null) return false;
        var wasOfflineAvailable = item.IsOfflineAvailable;
        db.Pages.Remove(item);
        await db.SaveChangesAsync();
        if (wasOfflineAvailable) await syncNotifier.NotifyAsync("masails");
        return true;
    }
}
