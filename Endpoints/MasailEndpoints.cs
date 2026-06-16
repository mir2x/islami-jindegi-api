using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class MasailEndpoints
{
    public static void MapMasailEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/masail");

        group.MapGet("/authors", async (AppDbContext db, bool published = true) =>
        {
            var data = await db.Authors
                .Select(a => new { a.Id, a.Name, Count = a.Masails.Count(m => m.Published == published) })
                .Where(a => a.Count > 0)
                .OrderByDescending(a => a.Count)
                .ThenBy(a => a.Name)
                .ToListAsync();
            return Results.Ok(data.Select(a => new MasailAuthorOption(a.Id, a.Name, a.Count)));
        });

        group.MapGet("/categories", async (AppDbContext db, bool published = true) =>
        {
            var data = await db.Categories
                .Where(c => c.ParentId == null)
                .Select(c => new { c.Id, c.Title, Count = c.Masails.Count(m => m.Published == published) })
                .Where(c => c.Count > 0)
                .OrderByDescending(c => c.Count)
                .ThenBy(c => c.Title)
                .ToListAsync();
            return Results.Ok(data.Select(c => new MasailCategoryOption(c.Id, c.Title, c.Count)));
        });

        group.MapGet("/", async (
            AppDbContext db,
            int page = 1,
            int pageSize = 10,
            string? search = null,
            Guid? authorId = null,
            Guid? categoryId = null,
            bool? published = null,
            bool? hasAudio = null) =>
        {
            var query = db.Masails
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

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(m => m.Position)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new PagedResult<MasailListItem>(data.Select(ToListItem), total, page, pageSize));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.Masails
                .Include(m => m.Author)
                .Include(m => m.Categories)
                .FirstOrDefaultAsync(m => m.Id == id);

            return item is null ? Results.NotFound() : Results.Ok(ToDetail(item));
        });

        group.MapPost("/", async (SaveMasailRequest req, AppDbContext db) =>
        {
            var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();
            var position = req.Position ?? (await db.Masails.MaxAsync(m => (int?)m.Position) ?? 0) + 1;

            var item = new Masail
            {
                Id = Guid.NewGuid(),
                Title = req.Title,
                Question = req.Question,
                Answer = req.Answer,
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
            db.Masails.Add(item);
            await db.SaveChangesAsync();
            if (item.AuthorId.HasValue)
                await db.Entry(item).Reference(m => m.Author).LoadAsync();
            return Results.Created($"/api/masail/{item.Id}", ToListItem(item));
        });

        group.MapPut("/{id:guid}", async (Guid id, SaveMasailRequest req, AppDbContext db) =>
        {
            var item = await db.Masails
                .Include(m => m.Author)
                .Include(m => m.Categories)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item is null) return Results.NotFound();

            var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();

            item.Title = req.Title;
            item.Question = req.Question;
            item.Answer = req.Answer;
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
            if (item.AuthorId.HasValue)
                await db.Entry(item).Reference(m => m.Author).LoadAsync();
            return Results.Ok(ToListItem(item));
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.Masails.FindAsync(id);
            if (item is null) return Results.NotFound();
            db.Masails.Remove(item);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static MasailListItem ToListItem(Masail m) => new(
        m.Id, m.Title, m.Language, m.HasAudio, m.AudioUrl,
        m.Published, m.PublishedAt, m.Position,
        m.CreatedAt, m.UpdatedAt,
        m.Author is null ? null : AuthorEndpoints.ToResponse(m.Author),
        m.Categories.Select(CategoryEndpoints.ToResponse).ToList());

    static MasailDetail ToDetail(Masail m) => new(
        m.Id, m.Title, m.Question, m.Answer, m.Language, m.HasAudio, m.AudioUrl, m.DocumentUrl,
        m.Published, m.PublishedAt, m.Position,
        m.CreatedAt, m.UpdatedAt,
        m.Author is null ? null : AuthorEndpoints.ToResponse(m.Author),
        m.Categories.Select(CategoryEndpoints.ToResponse).ToList());
}
