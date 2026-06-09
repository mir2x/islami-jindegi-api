using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class MalfuzatEndpoints
{
    public static void MapMalfuzatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/malfuzat");

        group.MapGet("/", async (
            AppDbContext db,
            int page = 1,
            int pageSize = 10,
            string? search = null,
            Guid? authorId = null,
            Guid? categoryId = null,
            bool? published = null) =>
        {
            var query = db.Malfuzats
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

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(m => m.Position)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new PagedResult<MalfuzatListItem>(data.Select(ToListItem), total, page, pageSize));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.Malfuzats
                .Include(m => m.Author)
                .Include(m => m.Categories)
                .FirstOrDefaultAsync(m => m.Id == id);

            return item is null ? Results.NotFound() : Results.Ok(ToDetail(item));
        });

        group.MapPost("/", async (SaveMalfuzatRequest req, AppDbContext db) =>
        {
            var author = await db.Authors.FindAsync(req.AuthorId);
            if (author is null) return Results.BadRequest("Author not found");

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
            return Results.Created($"/api/malfuzat/{item.Id}", ToListItem(item));
        });

        group.MapPut("/{id:guid}", async (Guid id, SaveMalfuzatRequest req, AppDbContext db) =>
        {
            var item = await db.Malfuzats
                .Include(m => m.Author)
                .Include(m => m.Categories)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item is null) return Results.NotFound();

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
            return Results.Ok(ToListItem(item));
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.Malfuzats.FindAsync(id);
            if (item is null) return Results.NotFound();
            db.Malfuzats.Remove(item);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static MalfuzatListItem ToListItem(Malfuzat m) => new(
        m.Id, m.Title, m.Excerpt, m.Language, m.HasAudio, m.AudioUrl,
        m.Published, m.PublishedAt, m.Position,
        m.CreatedAt, m.UpdatedAt,
        AuthorEndpoints.ToResponse(m.Author),
        m.Categories.Select(CategoryEndpoints.ToResponse).ToList());

    static MalfuzatDetail ToDetail(Malfuzat m) => new(
        m.Id, m.Title, m.Body, m.Excerpt, m.Language, m.HasAudio, m.AudioUrl, m.DocumentUrl,
        m.Published, m.PublishedAt, m.Position,
        m.CreatedAt, m.UpdatedAt,
        AuthorEndpoints.ToResponse(m.Author),
        m.Categories.Select(CategoryEndpoints.ToResponse).ToList());
}
