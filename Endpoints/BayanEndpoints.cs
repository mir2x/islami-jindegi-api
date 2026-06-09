using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class BayanEndpoints
{
    public static void MapBayanEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/bayan");

        group.MapGet("/", async (
            AppDbContext db,
            int page = 1,
            int pageSize = 10,
            string? search = null,
            Guid? authorId = null,
            Guid? categoryId = null,
            bool? published = null) =>
        {
            var query = db.Bayans
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

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(b => b.Position)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new PagedResult<BayanListItem>(data.Select(ToListItem), total, page, pageSize));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.Bayans
                .Include(b => b.Author)
                .Include(b => b.Categories)
                .FirstOrDefaultAsync(b => b.Id == id);

            return item is null ? Results.NotFound() : Results.Ok(ToDetail(item));
        });

        group.MapPost("/", async (SaveBayanRequest req, AppDbContext db) =>
        {
            var author = await db.Authors.FindAsync(req.AuthorId);
            if (author is null) return Results.BadRequest("Author not found");

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
            return Results.Created($"/api/bayan/{item.Id}", ToListItem(item));
        });

        group.MapPut("/{id:guid}", async (Guid id, SaveBayanRequest req, AppDbContext db) =>
        {
            var item = await db.Bayans
                .Include(b => b.Author)
                .Include(b => b.Categories)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (item is null) return Results.NotFound();

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
            return Results.Ok(ToListItem(item));
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.Bayans.FindAsync(id);
            if (item is null) return Results.NotFound();
            db.Bayans.Remove(item);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static BayanListItem ToListItem(Bayan b) => new(
        b.Id, b.Title, b.Excerpt, b.Language, b.Location, b.AudioUrl,
        b.Published, b.PublishedAt, b.Position,
        b.CreatedAt, b.UpdatedAt,
        AuthorEndpoints.ToResponse(b.Author),
        b.Categories.Select(CategoryEndpoints.ToResponse).ToList());

    static BayanDetail ToDetail(Bayan b) => new(
        b.Id, b.Title, b.Excerpt, b.Language, b.Location, b.AudioUrl,
        b.Published, b.PublishedAt, b.Position,
        b.CreatedAt, b.UpdatedAt,
        AuthorEndpoints.ToResponse(b.Author),
        b.Categories.Select(CategoryEndpoints.ToResponse).ToList());
}
