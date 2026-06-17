using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class NamazTimeService(AppDbContext db) : INamazTimeService
{
    public async Task<PagedResult<NamazTimeListItem>> GetListAsync(int page, int pageSize, string? search)
    {
        var query = db.NamazTimes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(n => n.Title.Contains(search) || (n.TitleBn != null && n.TitleBn.Contains(search)));

        var total = await query.CountAsync();
        var data = await query
            .OrderBy(n => n.Position)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NamazTimeListItem(n.Id, n.Title, n.TitleBn, n.Position, n.CreatedAt, n.UpdatedAt))
            .ToListAsync();

        return new PagedResult<NamazTimeListItem>(data, total, page, pageSize);
    }

    public async Task<NamazTimeDetail?> GetByIdAsync(Guid id)
    {
        var item = await db.NamazTimes.FindAsync(id);
        return item is null ? null : Mappers.ToNamazTimeDetail(item);
    }

    public async Task<NamazTimeDetail> CreateAsync(SaveNamazTimeRequest req)
    {
        var position = req.Position ?? (await db.NamazTimes.MaxAsync(n => (int?)n.Position) ?? 0) + 1;
        var item = new NamazTime
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            TitleBn = req.TitleBn,
            Masail = req.Masail,
            Fazail = req.Fazail,
            Position = position,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.NamazTimes.Add(item);
        await db.SaveChangesAsync();
        return Mappers.ToNamazTimeDetail(item);
    }

    public async Task<NamazTimeDetail?> UpdateAsync(Guid id, SaveNamazTimeRequest req)
    {
        var item = await db.NamazTimes.FindAsync(id);
        if (item is null) return null;

        item.Title = req.Title;
        item.TitleBn = req.TitleBn;
        item.Masail = req.Masail;
        item.Fazail = req.Fazail;
        if (req.Position.HasValue) item.Position = req.Position.Value;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Mappers.ToNamazTimeDetail(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await db.NamazTimes.FindAsync(id);
        if (item is null) return false;
        db.NamazTimes.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }
}
