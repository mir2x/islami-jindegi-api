using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class NewsEndpoints
{
    public static void MapNewsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/news");

        group.MapGet("/", async (
            AppDbContext db,
            int page = 1,
            int pageSize = 20,
            string? search = null,
            bool? published = null) =>
        {
            var query = db.News.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(n => n.Title.Contains(search));
            if (published.HasValue)
                query = query.Where(n => n.Published == published.Value);

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(n => n.Position)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NewsListItem(n.Id, n.Title, n.Excerpt, n.Language, n.Published, n.PublishedAt, n.Position, n.CreatedAt, n.UpdatedAt))
                .ToListAsync();

            return Results.Ok(new PagedResult<NewsListItem>(data, total, page, pageSize));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.News.FindAsync(id);
            return item is null ? Results.NotFound() : Results.Ok(ToDetail(item));
        });

        group.MapPost("/", async (SaveNewsRequest req, AppDbContext db) =>
        {
            var position = req.Position ?? (await db.News.MaxAsync(n => (int?)n.Position) ?? 0) + 1;
            var item = new News
            {
                Id = Guid.NewGuid(),
                Title = req.Title,
                Body = req.Body,
                Excerpt = req.Excerpt,
                Language = req.Language,
                Published = req.Published,
                PublishedAt = req.PublishedAt,
                Position = position,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.News.Add(item);
            await db.SaveChangesAsync();
            return Results.Created($"/api/news/{item.Id}", ToDetail(item));
        });

        group.MapPut("/{id:guid}", async (Guid id, SaveNewsRequest req, AppDbContext db) =>
        {
            var item = await db.News.FindAsync(id);
            if (item is null) return Results.NotFound();

            item.Title = req.Title;
            item.Body = req.Body;
            item.Excerpt = req.Excerpt;
            item.Language = req.Language;
            item.Published = req.Published;
            item.PublishedAt = req.PublishedAt;
            if (req.Position.HasValue) item.Position = req.Position.Value;
            item.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToDetail(item));
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var item = await db.News.FindAsync(id);
            if (item is null) return Results.NotFound();
            db.News.Remove(item);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static NewsDetail ToDetail(News n) => new(
        n.Id, n.Title, n.Body, n.Excerpt, n.Language,
        n.Published, n.PublishedAt, n.Position, n.CreatedAt, n.UpdatedAt);
}
