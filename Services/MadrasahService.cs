using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class MadrasahService(AppDbContext db, ContentSyncNotifier syncNotifier) : IMadrasahService
{
    public async Task<PagedResult<MadrasahListItem>> GetListAsync(int page, int pageSize, string? search, bool? offlineAvailable = null)
    {
        var query = db.Madrasahs
            .AsNoTracking()
            .Include(m => m.Infos)
            .Include(m => m.Photos)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Title.Contains(search));
        if (offlineAvailable.HasValue)
            query = query.Where(m => m.IsOfflineAvailable == offlineAvailable.Value);

        var total = await query.CountAsync();
        var data = await query
            .OrderBy(m => m.Position).ThenBy(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<MadrasahListItem>(
            data.Select(m => new MadrasahListItem(m.Id, m.Title, m.Excerpt, m.Position, m.Infos.Count, m.Photos.Count, m.IsOfflineAvailable, m.CreatedAt, m.UpdatedAt)),
            total, page, pageSize);
    }

    public async Task<MadrasahDetail?> GetByIdAsync(Guid id)
    {
        var item = await db.Madrasahs
            .AsNoTracking()
            .Include(m => m.Infos.OrderBy(i => i.Position))
            .Include(m => m.Photos.OrderBy(p => p.Position))
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.Id == id);
        if (item is null) return null;
        var previous = await db.Madrasahs.AsNoTracking()
            .Where(m => m.Position < item.Position || (m.Position == item.Position && m.Id.CompareTo(item.Id) < 0))
            .OrderByDescending(m => m.Position).ThenByDescending(m => m.Id)
            .Select(m => new SiblingRef(m.Id, m.Title, m.Position)).FirstOrDefaultAsync();
        var next = await db.Madrasahs.AsNoTracking()
            .Where(m => m.Position > item.Position || (m.Position == item.Position && m.Id.CompareTo(item.Id) > 0))
            .OrderBy(m => m.Position).ThenBy(m => m.Id)
            .Select(m => new SiblingRef(m.Id, m.Title, m.Position)).FirstOrDefaultAsync();
        return Mappers.ToMadrasahDetail(item) with { Previous = previous, Next = next };
    }

    public async Task<IEnumerable<MadrasahDetail>> GetOfflineSyncAsync(DateTime? since)
    {
        // Infos/Photos have no independent CRUD endpoint of their own — they're
        // only ever edited via UpdateAsync below, which always stamps the parent
        // too, so filtering on the parent's UpdatedAt alone is sufficient here
        // (unlike Books, where Chapters have their own controller/endpoints).
        var items = await db.Madrasahs
            .AsNoTracking()
            .Where(m => m.IsOfflineAvailable && (since == null || m.UpdatedAt > since))
            .Include(m => m.Infos.OrderBy(i => i.Position))
            .Include(m => m.Photos.OrderBy(p => p.Position))
            .AsSplitQuery()
            .ToListAsync();
        return items.Select(Mappers.ToMadrasahDetail);
    }

    public async Task<List<Guid>> GetOfflineIdsAsync()
        => await db.Madrasahs.Where(m => m.IsOfflineAvailable).Select(m => m.Id).ToListAsync();

    public async Task<MadrasahDetail> CreateAsync(SaveMadrasahRequest req)
    {
        var position = req.Position ?? (await db.Madrasahs.MaxAsync(m => (int?)m.Position) ?? 0) + 1;
        var now = DateTime.UtcNow;

        var item = new Madrasah
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Excerpt = req.Excerpt,
            Introduction = req.Introduction,
            Position = position,
            CreatedAt = now,
            UpdatedAt = now,
            Infos = req.Infos.Select(i => new MadrasahInfo { Id = Guid.NewGuid(), Label = i.Label, Info = i.Info, Position = i.Position, CreatedAt = now, UpdatedAt = now }).ToList(),
            Photos = req.Photos.Select(p => new MadrasahPhoto { Id = Guid.NewGuid(), Title = p.Title, ImageUrl = p.ImageUrl, Position = p.Position, CreatedAt = now, UpdatedAt = now }).ToList()
        };
        db.Madrasahs.Add(item);
        await db.SaveChangesAsync();
        return Mappers.ToMadrasahDetail(item);
    }

    public async Task<MadrasahDetail?> UpdateAsync(Guid id, SaveMadrasahRequest req)
    {
        var item = await db.Madrasahs
            .Include(m => m.Infos)
            .Include(m => m.Photos)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (item is null) return null;

        var now = DateTime.UtcNow;
        item.Title = req.Title;
        item.Excerpt = req.Excerpt;
        item.Introduction = req.Introduction;
        if (req.Position.HasValue) item.Position = req.Position.Value;
        item.UpdatedAt = now;

        db.MadrasahInfos.RemoveRange(item.Infos);
        db.MadrasahPhotos.RemoveRange(item.Photos);

        item.Infos = req.Infos.Select(i => new MadrasahInfo { Id = Guid.NewGuid(), Label = i.Label, Info = i.Info, Position = i.Position, MadrasahId = item.Id, CreatedAt = now, UpdatedAt = now }).ToList();
        item.Photos = req.Photos.Select(p => new MadrasahPhoto { Id = Guid.NewGuid(), Title = p.Title, ImageUrl = p.ImageUrl, Position = p.Position, MadrasahId = item.Id, CreatedAt = now, UpdatedAt = now }).ToList();

        await db.SaveChangesAsync();
        if (item.IsOfflineAvailable) await syncNotifier.NotifyAsync("madrasahs");
        return Mappers.ToMadrasahDetail(item);
    }

    public async Task<MadrasahListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable)
    {
        var item = await db.Madrasahs
            .Include(m => m.Infos)
            .Include(m => m.Photos)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (item is null) return null;

        item.IsOfflineAvailable = isOfflineAvailable;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await syncNotifier.NotifyAsync("madrasahs");
        return new MadrasahListItem(item.Id, item.Title, item.Excerpt, item.Position, item.Infos.Count, item.Photos.Count, item.IsOfflineAvailable, item.CreatedAt, item.UpdatedAt);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await db.Madrasahs.FindAsync(id);
        if (item is null) return false;
        var wasOfflineAvailable = item.IsOfflineAvailable;
        db.Madrasahs.Remove(item);
        await db.SaveChangesAsync();
        if (wasOfflineAvailable) await syncNotifier.NotifyAsync("madrasahs");
        return true;
    }
}
