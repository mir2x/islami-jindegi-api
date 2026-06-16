using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class BookEndpoints
{
    public static void MapBookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/books");

        group.MapGet("/", async (
            AppDbContext db,
            int page = 1,
            int pageSize = 10,
            string? search = null,
            Guid? authorId = null,
            Guid? categoryId = null,
            bool? published = null) =>
        {
            var query = db.Books
                .Include(b => b.Authors)
                .Include(b => b.Categories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Title.Contains(search));

            if (authorId.HasValue)
                query = query.Where(b => b.Authors.Any(a => a.Id == authorId));

            if (categoryId.HasValue)
                query = query.Where(b => b.Categories.Any(c => c.Id == categoryId));

            if (published.HasValue)
                query = query.Where(b => b.Published == published.Value);

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(b => b.Position)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    Book = b,
                    ChapterCount = b.Chapters.Count()
                })
                .ToListAsync();

            return Results.Ok(new PagedResult<BookListItem>(
                data.Select(x => ToListItem(x.Book, x.ChapterCount)), total, page, pageSize));
        });

        group.MapGet("/authors", async (AppDbContext db, bool published = true) =>
        {
            var data = await db.Authors
                .Select(a => new { a.Id, a.Name, Count = a.Books.Count(b => b.Published == published) })
                .Where(a => a.Count > 0)
                .OrderByDescending(a => a.Count)
                .ThenBy(a => a.Name)
                .ToListAsync();
            return Results.Ok(data.Select(a => new BookAuthorOption(a.Id, a.Name, a.Count)));
        });

        group.MapGet("/categories", async (AppDbContext db, bool published = true) =>
        {
            var data = await db.Categories
                .Where(c => c.ParentId == null)
                .Select(c => new { c.Id, c.Title, Count = c.Books.Count(b => b.Published == published) })
                .Where(c => c.Count > 0)
                .OrderByDescending(c => c.Count)
                .ThenBy(c => c.Title)
                .ToListAsync();
            return Results.Ok(data.Select(c => new BookCategoryOption(c.Id, c.Title, c.Count)));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var book = await db.Books
                .Include(b => b.Authors)
                .Include(b => b.Categories).ThenInclude(c => c.Children)
                .Include(b => b.Chapters).ThenInclude(c => c.SubChapters)
                .AsSplitQuery()
                .FirstOrDefaultAsync(b => b.Id == id);

            return book is null ? Results.NotFound() : Results.Ok(ToDetail(book));
        });

        group.MapPost("/", async (SaveBookRequest req, AppDbContext db) =>
        {
            var authors = await db.Authors.Where(a => req.AuthorIds.Contains(a.Id)).ToListAsync();
            var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();
            var position = req.Position ?? (await db.Books.MaxAsync(b => (int?)b.Position) ?? 0) + 1;

            var book = new Book
            {
                Id = Guid.NewGuid(),
                Title = req.Title,
                Excerpt = req.Excerpt,
                Publisher = req.Publisher,
                Price = req.Price,
                Language = req.Language,
                CoverUrl = req.CoverUrl,
                DocumentUrl = req.DocumentUrl,
                Position = position,
                PublishedAt = req.PublishedAt,
                Published = req.Published,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Authors = authors,
                Categories = categories
            };
            db.Books.Add(book);
            await db.SaveChangesAsync();
            return Results.Created($"/api/books/{book.Id}", ToListItem(book));
        });

        group.MapPut("/{id:guid}", async (Guid id, SaveBookRequest req, AppDbContext db) =>
        {
            var book = await db.Books
                .Include(b => b.Authors)
                .Include(b => b.Categories)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book is null) return Results.NotFound();

            var authors = await db.Authors.Where(a => req.AuthorIds.Contains(a.Id)).ToListAsync();
            var categories = await db.Categories.Where(c => req.CategoryIds.Contains(c.Id)).ToListAsync();

            book.Title = req.Title;
            book.Excerpt = req.Excerpt;
            book.Publisher = req.Publisher;
            book.Price = req.Price;
            book.Language = req.Language;
            book.CoverUrl = req.CoverUrl;
            book.DocumentUrl = req.DocumentUrl;
            book.PublishedAt = req.PublishedAt;
            book.Published = req.Published;
            if (req.Position.HasValue) book.Position = req.Position.Value;
            book.Authors = authors;
            book.Categories = categories;
            book.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToListItem(book));
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var book = await db.Books.FindAsync(id);
            if (book is null) return Results.NotFound();

            db.Books.Remove(book);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static BookListItem ToListItem(Book b, int chapterCount = 0) => new(
        b.Id, b.Title, b.Excerpt, b.Publisher, b.Price, b.Language,
        b.CoverUrl, b.DocumentUrl, b.Position, b.PublishedAt, b.Published,
        b.CreatedAt, b.UpdatedAt,
        b.Authors.Select(AuthorEndpoints.ToResponse).ToList(),
        b.Categories.Select(CategoryEndpoints.ToResponse).ToList(),
        chapterCount);

    static BookDetail ToDetail(Book b) => new(
        b.Id, b.Title, b.Excerpt, b.Publisher, b.Price, b.Language,
        b.CoverUrl, b.DocumentUrl, b.Position, b.PublishedAt, b.Published,
        b.CreatedAt, b.UpdatedAt,
        b.Authors.Select(AuthorEndpoints.ToResponse).ToList(),
        b.Categories.Select(CategoryEndpoints.ToResponse).ToList(),
        b.Chapters.OrderBy(c => c.Position).Select(ChapterEndpoints.ToResponse).ToList());
}
