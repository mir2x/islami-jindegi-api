using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class ArticleEndpoints
{
    public static void MapArticleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/articles");

        group.MapGet("/", async (
            AppDbContext db,
            int page = 1,
            int pageSize = 10,
            string? search = null,
            Guid? authorId = null,
            Guid? categoryId = null,
            bool? published = null) =>
        {
            var query = db.Articles
                .Include(a => a.Author)
                .Include(a => a.Categories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.Title.Contains(search));
            if (authorId.HasValue)
                query = query.Where(a => a.AuthorId == authorId.Value);
            if (categoryId.HasValue)
                query = query.Where(a => a.Categories.Any(c => c.Id == categoryId.Value));
            if (published.HasValue)
                query = query.Where(a => a.Published == published.Value);

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(a => a.Position)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new PagedResult<ArticleListItem>(data.Select(ToListItem), total, page, pageSize));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.Articles
                .Include(a => a.Author)
                .Include(a => a.Categories)
                .FirstOrDefaultAsync(a => a.Id == id);

            return item is null ? Results.NotFound() : Results.Ok(ToDetail(item));
        });

        group.MapPost("/", async (SaveArticleRequest req, AppDbContext db) =>
        {
            var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();
            var position = req.Position ?? (await db.Articles.MaxAsync(a => (int?)a.Position) ?? 0) + 1;

            var item = new Article
            {
                Id = Guid.NewGuid(),
                Title = req.Title,
                Body = req.Body,
                Excerpt = req.Excerpt,
                Language = req.Language,
                DocumentUrl = req.DocumentUrl,
                Published = req.Published,
                PublishedAt = req.PublishedAt,
                Position = position,
                AuthorId = req.AuthorId,
                Categories = categories,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Articles.Add(item);
            await db.SaveChangesAsync();
            if (item.AuthorId.HasValue)
                await db.Entry(item).Reference(a => a.Author).LoadAsync();
            return Results.Created($"/api/articles/{item.Id}", ToListItem(item));
        });

        group.MapPut("/{id:guid}", async (Guid id, SaveArticleRequest req, AppDbContext db) =>
        {
            var item = await db.Articles
                .Include(a => a.Author)
                .Include(a => a.Categories)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (item is null) return Results.NotFound();

            var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();

            item.Title = req.Title;
            item.Body = req.Body;
            item.Excerpt = req.Excerpt;
            item.Language = req.Language;
            item.DocumentUrl = req.DocumentUrl;
            item.Published = req.Published;
            item.PublishedAt = req.PublishedAt;
            if (req.Position.HasValue) item.Position = req.Position.Value;
            item.AuthorId = req.AuthorId;
            item.Categories = categories;
            item.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            if (item.AuthorId.HasValue)
                await db.Entry(item).Reference(a => a.Author).LoadAsync();
            return Results.Ok(ToListItem(item));
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.Articles.FindAsync(id);
            if (item is null) return Results.NotFound();
            db.Articles.Remove(item);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static ArticleListItem ToListItem(Article a) => new(
        a.Id, a.Title, a.Excerpt, a.Language,
        a.Published, a.PublishedAt, a.Position,
        a.CreatedAt, a.UpdatedAt,
        a.Author is null ? null : AuthorEndpoints.ToResponse(a.Author),
        a.Categories.Select(CategoryEndpoints.ToResponse).ToList());

    static ArticleDetail ToDetail(Article a) => new(
        a.Id, a.Title, a.Body, a.Excerpt, a.Language, a.DocumentUrl,
        a.Published, a.PublishedAt, a.Position,
        a.CreatedAt, a.UpdatedAt,
        a.Author is null ? null : AuthorEndpoints.ToResponse(a.Author),
        a.Categories.Select(CategoryEndpoints.ToResponse).ToList());
}
