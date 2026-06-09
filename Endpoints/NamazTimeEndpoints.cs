using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class NamazTimeEndpoints
{
    public static void MapNamazTimeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/namaz-times");

        group.MapGet("/", async (
            AppDbContext db,
            int page = 1,
            int pageSize = 20,
            string? search = null) =>
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

            return Results.Ok(new PagedResult<NamazTimeListItem>(data, total, page, pageSize));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.NamazTimes.FindAsync(id);
            return item is null ? Results.NotFound() : Results.Ok(ToDetail(item));
        });

        group.MapPost("/", async (SaveNamazTimeRequest req, AppDbContext db) =>
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
            return Results.Created($"/api/namaz-times/{item.Id}", ToDetail(item));
        });

        group.MapPut("/{id:guid}", async (Guid id, SaveNamazTimeRequest req, AppDbContext db) =>
        {
            var item = await db.NamazTimes.FindAsync(id);
            if (item is null) return Results.NotFound();

            item.Title = req.Title;
            item.TitleBn = req.TitleBn;
            item.Masail = req.Masail;
            item.Fazail = req.Fazail;
            if (req.Position.HasValue) item.Position = req.Position.Value;
            item.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToDetail(item));
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.NamazTimes.FindAsync(id);
            if (item is null) return Results.NotFound();
            db.NamazTimes.Remove(item);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static NamazTimeDetail ToDetail(NamazTime n) => new(
        n.Id, n.Title, n.TitleBn, n.Masail, n.Fazail, n.Position, n.CreatedAt, n.UpdatedAt);
}
