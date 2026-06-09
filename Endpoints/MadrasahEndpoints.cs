using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class MadrasahEndpoints
{
    public static void MapMadrasahEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/madrasahs");

        group.MapGet("/", async (
            AppDbContext db,
            int page = 1,
            int pageSize = 20,
            string? search = null) =>
        {
            var query = db.Madrasahs
                .Include(m => m.Infos)
                .Include(m => m.Photos)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.Title.Contains(search));

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(m => m.Position)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new PagedResult<MadrasahListItem>(
                data.Select(m => new MadrasahListItem(m.Id, m.Title, m.Excerpt, m.Position, m.Infos.Count, m.Photos.Count, m.CreatedAt, m.UpdatedAt)),
                total, page, pageSize));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.Madrasahs
                .Include(m => m.Infos.OrderBy(i => i.Position))
                .Include(m => m.Photos.OrderBy(p => p.Position))
                .FirstOrDefaultAsync(m => m.Id == id);
            return item is null ? Results.NotFound() : Results.Ok(ToDetail(item));
        });

        group.MapPost("/", async (SaveMadrasahRequest req, AppDbContext db) =>
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
            return Results.Created($"/api/madrasahs/{item.Id}", ToDetail(item));
        });

        group.MapPut("/{id:guid}", async (Guid id, SaveMadrasahRequest req, AppDbContext db) =>
        {
            var item = await db.Madrasahs
                .Include(m => m.Infos)
                .Include(m => m.Photos)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item is null) return Results.NotFound();

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
            return Results.Ok(ToDetail(item));
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.Madrasahs.FindAsync(id);
            if (item is null) return Results.NotFound();
            db.Madrasahs.Remove(item);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static MadrasahDetail ToDetail(Madrasah m) => new(
        m.Id, m.Title, m.Excerpt, m.Introduction, m.Position,
        m.Infos.OrderBy(i => i.Position).Select(i => new MadrasahInfoItem(i.Id, i.Label, i.Info, i.Position)).ToList(),
        m.Photos.OrderBy(p => p.Position).Select(p => new MadrasahPhotoItem(p.Id, p.Title, p.ImageUrl, p.Position)).ToList(),
        m.CreatedAt, m.UpdatedAt);
}
