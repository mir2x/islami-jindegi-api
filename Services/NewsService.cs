using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class NewsService(AppDbContext db) : INewsService
{
    public async Task<PagedResult<NewsListItem>> GetListAsync(int page, int pageSize, string? search, bool? published, string? sort)
    {
        var query = db.News.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(n => n.Title.Contains(search));
        if (published.HasValue)
            query = query.Where(n => n.Published == published.Value);

        var orderedQuery = sort switch
        {
            "position_desc" => query.OrderByDescending(n => n.Position),
            "position_asc" => query.OrderBy(n => n.Position),
            "title_asc" => query.OrderBy(n => n.Title),
            "title_desc" => query.OrderByDescending(n => n.Title),
            "language_asc" => query.OrderBy(n => n.Language),
            "language_desc" => query.OrderByDescending(n => n.Language),
            "date_asc" => query.OrderBy(n => n.PublishedAt),
            "date_desc" => query.OrderByDescending(n => n.PublishedAt),
            "published_asc" => query.OrderBy(n => n.Published).ThenBy(n => n.Position),
            "published_desc" => query.OrderByDescending(n => n.Published).ThenBy(n => n.Position),
            _ => query.OrderBy(n => n.Position),
        };

        var total = await query.CountAsync();
        var data = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NewsListItem(n.Id, n.Title, n.Excerpt, n.Language, n.Published, n.PublishedAt, n.Position, n.CreatedAt, n.UpdatedAt))
            .ToListAsync();

        return new PagedResult<NewsListItem>(data, total, page, pageSize);
    }

    public async Task<NewsDetail?> GetByIdAsync(Guid id)
    {
        var item = await db.News.FindAsync(id);
        return item is null ? null : Mappers.ToNewsDetail(item);
    }

    public async Task<NewsDetail> CreateAsync(SaveNewsRequest req)
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
        return Mappers.ToNewsDetail(item);
    }

    public async Task<NewsDetail?> UpdateAsync(Guid id, SaveNewsRequest req)
    {
        var item = await db.News.FindAsync(id);
        if (item is null) return null;

        item.Title = req.Title;
        item.Body = req.Body;
        item.Excerpt = req.Excerpt;
        item.Language = req.Language;
        item.Published = req.Published;
        item.PublishedAt = req.PublishedAt;
        if (req.Position.HasValue) item.Position = req.Position.Value;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Mappers.ToNewsDetail(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await db.News.FindAsync(id);
        if (item is null) return false;
        db.News.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }
}
