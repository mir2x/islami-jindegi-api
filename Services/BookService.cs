using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class BookService(AppDbContext db, ContentSyncNotifier syncNotifier) : IBookService
{
    public async Task<PagedResult<BookListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, bool? offlineAvailable, string? sort)
    {
        var query = db.Books
            .AsNoTracking()
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(b => b.Title.Contains(search));
        if (authorId.HasValue)
            query = query.Where(b => b.Authors.Any(a => a.Id == authorId));
        if (categoryId.HasValue)
            query = query.Where(b => b.Categories.Any(c => c.Id == categoryId));
        if (published.HasValue)
            query = query.Where(b => b.Published == published.Value);
        if (offlineAvailable.HasValue)
            query = query.Where(b => b.IsOfflineAvailable == offlineAvailable.Value);

        var orderedQuery = sort switch
        {
            "position_desc" => query.OrderByDescending(b => b.Position),
            "position_asc" => query.OrderBy(b => b.Position),
            "title_asc" => query.OrderBy(b => b.Title),
            "title_desc" => query.OrderByDescending(b => b.Title),
            "authors_asc" => query.OrderBy(b => b.Authors.OrderBy(a => a.Name).Select(a => a.Name).FirstOrDefault()),
            "authors_desc" => query.OrderByDescending(b => b.Authors.OrderBy(a => a.Name).Select(a => a.Name).FirstOrDefault()),
            "updated_asc" => query.OrderBy(b => b.UpdatedAt),
            "updated_desc" => query.OrderByDescending(b => b.UpdatedAt),
            "published_asc" => query.OrderBy(b => b.Published).ThenBy(b => b.Position),
            "published_desc" => query.OrderByDescending(b => b.Published).ThenBy(b => b.Position),
            _ => query.OrderBy(b => b.Position),
        };

        var total = await query.CountAsync();
        var data = await orderedQuery.ThenBy(b => b.Id)
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

    public async Task<BookDetail?> GetByIdAsync(Guid id, bool includeUnpublished = false)
    {
        var book = await db.Books
            .AsNoTracking()
            .Include(b => b.Authors)
            .Include(b => b.Categories).ThenInclude(c => c.Children)
            .Include(b => b.Chapters).ThenInclude(c => c.SubChapters)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.Id == id && (includeUnpublished || b.Published));
        if (book is null) return null;

        // ASC sequence: previous seeks downward, next seeks upward.
        var previous = await db.Books.AsNoTracking()
            .Where(b => b.Published && (b.Position < book.Position
                || (b.Position == book.Position && b.Id.CompareTo(book.Id) < 0)))
            .OrderByDescending(b => b.Position).ThenByDescending(b => b.Id)
            .Select(b => new SiblingRef(b.Id, b.Title, b.Position))
            .FirstOrDefaultAsync();
        var next = await db.Books.AsNoTracking()
            .Where(b => b.Published && (b.Position > book.Position
                || (b.Position == book.Position && b.Id.CompareTo(book.Id) > 0)))
            .OrderBy(b => b.Position).ThenBy(b => b.Id)
            .Select(b => new SiblingRef(b.Id, b.Title, b.Position))
            .FirstOrDefaultAsync();
        return Mappers.ToBookDetail(book) with { Previous = previous, Next = next };
    }

    public async Task<IEnumerable<BookDetail>> GetOfflineSyncAsync(DateTime? since)
    {
        // Chapters/SubChapters carry their own UpdatedAt and editing one doesn't
        // bump the parent Book — so a chapter-only edit must still surface the
        // book here, otherwise the client's delta sync would miss it.
        var books = await db.Books
            .AsNoTracking()
            .Where(b => b.Published && b.IsOfflineAvailable && (since == null
                || b.UpdatedAt > since
                || b.Chapters.Any(c => c.UpdatedAt > since)
                || b.Chapters.Any(c => c.SubChapters.Any(s => s.UpdatedAt > since))))
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .Include(b => b.Chapters).ThenInclude(c => c.SubChapters)
            .AsSplitQuery()
            .ToListAsync();
        return books.Select(Mappers.ToBookDetail);
    }

    public async Task<List<Guid>> GetOfflineIdsAsync()
        => await db.Books.Where(b => b.Published && b.IsOfflineAvailable).Select(b => b.Id).ToListAsync();

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
        if (book.IsOfflineAvailable) await syncNotifier.NotifyAsync("books");
        return Mappers.ToBookListItem(book);
    }

    public async Task<BookListItem?> SetOfflineAvailabilityAsync(Guid id, bool isOfflineAvailable)
    {
        var book = await db.Books
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (book is null) return null;

        book.IsOfflineAvailable = isOfflineAvailable;
        book.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await syncNotifier.NotifyAsync("books");
        return Mappers.ToBookListItem(book);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var book = await db.Books.FindAsync(id);
        if (book is null) return false;
        var wasOfflineAvailable = book.IsOfflineAvailable;
        db.Books.Remove(book);
        await db.SaveChangesAsync();
        if (wasOfflineAvailable) await syncNotifier.NotifyAsync("books");
        return true;
    }
}
