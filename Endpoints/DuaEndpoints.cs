using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class DuaEndpoints
{
    public static void MapDuaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dua");

        group.MapGet("/", async (
            AppDbContext db,
            int page = 1,
            int pageSize = 10,
            string? search = null,
            Guid? categoryId = null,
            bool? published = null) =>
        {
            var query = db.Duas
                .Include(d => d.Categories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(d => d.Title.Contains(search));
            if (categoryId.HasValue)
                query = query.Where(d => d.Categories.Any(c => c.Id == categoryId.Value));
            if (published.HasValue)
                query = query.Where(d => d.Published == published.Value);

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(d => d.Position)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new PagedResult<DuaListItem>(data.Select(ToListItem), total, page, pageSize));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.Duas
                .Include(d => d.Categories)
                .FirstOrDefaultAsync(d => d.Id == id);

            return item is null ? Results.NotFound() : Results.Ok(ToDetail(item));
        });

        group.MapPost("/", async (SaveDuaRequest req, AppDbContext db) =>
        {
            var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();
            var position = req.Position ?? (await db.Duas.MaxAsync(d => (int?)d.Position) ?? 0) + 1;

            var item = new Dua
            {
                Id = Guid.NewGuid(),
                Title = req.Title,
                Body = req.Body,
                Excerpt = req.Excerpt,
                Language = req.Language,
                AudioUrl = req.AudioUrl,
                DocumentUrl = req.DocumentUrl,
                Published = req.Published,
                Position = position,
                Categories = categories,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Duas.Add(item);
            await db.SaveChangesAsync();
            return Results.Created($"/api/dua/{item.Id}", ToListItem(item));
        });

        group.MapPut("/{id:guid}", async (Guid id, SaveDuaRequest req, AppDbContext db) =>
        {
            var item = await db.Duas
                .Include(d => d.Categories)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (item is null) return Results.NotFound();

            var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();

            item.Title = req.Title;
            item.Body = req.Body;
            item.Excerpt = req.Excerpt;
            item.Language = req.Language;
            item.AudioUrl = req.AudioUrl;
            item.DocumentUrl = req.DocumentUrl;
            item.Published = req.Published;
            if (req.Position.HasValue) item.Position = req.Position.Value;
            item.Categories = categories;
            item.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToListItem(item));
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.Duas.FindAsync(id);
            if (item is null) return Results.NotFound();
            db.Duas.Remove(item);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static DuaListItem ToListItem(Dua d) => new(
        d.Id, d.Title, d.Excerpt, d.Language, d.AudioUrl,
        d.Published, d.Position, d.CreatedAt, d.UpdatedAt,
        d.Categories.Select(CategoryEndpoints.ToResponse).ToList());

    static DuaDetail ToDetail(Dua d) => new(
        d.Id, d.Title, d.Body, d.Excerpt, d.Language, d.AudioUrl, d.DocumentUrl,
        d.Published, d.Position, d.CreatedAt, d.UpdatedAt,
        d.Categories.Select(CategoryEndpoints.ToResponse).ToList());
}
