using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class BookService(AppDbContext db) : IBookService
{
    public async Task<PagedResult<BookListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, string? sort)
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

        query = sort == "position_desc"
            ? query.OrderByDescending(b => b.Position)
            : query.OrderBy(b => b.Position);

        var total = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new { Book = b, ChapterCount = b.Chapters.Count() })
            .ToListAsync();

        return new PagedResult<BookListItem>(
            data.Select(x => Mappers.ToBookListItem(x.Book, x.ChapterCount)), total, page, pageSize);
    }

    public async Task<IEnumerable<BookAuthorOption>> GetAuthorsAsync(bool published, string? search = null, int? page = null, int? pageSize = null)
    {
        var query = db.Authors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Name.Contains(search));

        var projected = query
            .Select(a => new { a.Id, a.Name, Count = a.Books.Count(b => b.Published == published) })
            .Where(a => a.Count > 0)
            .OrderByDescending(a => a.Count)
            .ThenBy(a => a.Name);

        var sliced = page.HasValue && pageSize.HasValue
            ? projected.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value)
            : projected;

        var data = await sliced.ToListAsync();
        return data.Select(a => new BookAuthorOption(a.Id, a.Name, a.Count));
    }

    public async Task<IEnumerable<BookCategoryOption>> GetCategoriesAsync(bool published, string? search = null, int? page = null, int? pageSize = null)
    {
        var query = db.Categories.Where(c => c.ParentId == null);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search));

        var projected = query
            .Select(c => new { c.Id, c.Title, Count = c.Books.Count(b => b.Published == published) })
            .Where(c => c.Count > 0)
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Title);

        var sliced = page.HasValue && pageSize.HasValue
            ? projected.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value)
            : projected;

        var data = await sliced.ToListAsync();
        return data.Select(c => new BookCategoryOption(c.Id, c.Title, c.Count));
    }

    public async Task<BookDetail?> GetByIdAsync(Guid id)
    {
        var book = await db.Books
            .Include(b => b.Authors)
            .Include(b => b.Categories).ThenInclude(c => c.Children)
            .Include(b => b.Chapters).ThenInclude(c => c.SubChapters)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.Id == id);
        return book is null ? null : Mappers.ToBookDetail(book);
    }

    public async Task<BookListItem> CreateAsync(SaveBookRequest req)
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
        return Mappers.ToBookListItem(book);
    }

    public async Task<BookListItem?> UpdateAsync(Guid id, SaveBookRequest req)
    {
        var book = await db.Books
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (book is null) return null;

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
        return Mappers.ToBookListItem(book);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var book = await db.Books.FindAsync(id);
        if (book is null) return false;
        db.Books.Remove(book);
        await db.SaveChangesAsync();
        return true;
    }
}
