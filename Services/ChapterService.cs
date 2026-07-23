using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class ChapterService(AppDbContext db) : IChapterService
{
    public async Task<PagedResult<ChapterListItem>> GetChaptersAsync(int page, int pageSize, Guid? bookId, string? search, string? sort)
    {
        var query = db.Chapters.Include(c => c.Book).Include(c => c.SubChapters).AsQueryable();
        if (bookId.HasValue) query = query.Where(c => c.BookId == bookId.Value);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.Title.Contains(search));

        // Default (no/unknown sort) groups chapters under their book, then orders within it.
        var ordered = sort switch
        {
            "position_desc" => query.OrderByDescending(c => c.Position),
            "position_asc" => query.OrderBy(c => c.Position),
            "title_asc" => query.OrderBy(c => c.Title),
            "title_desc" => query.OrderByDescending(c => c.Title),
            "book_asc" => query.OrderBy(c => c.Book.Title).ThenBy(c => c.Position),
            "book_desc" => query.OrderByDescending(c => c.Book.Title).ThenBy(c => c.Position),
            "subs_asc" => query.OrderBy(c => c.SubChapters.Count).ThenBy(c => c.Position),
            "subs_desc" => query.OrderByDescending(c => c.SubChapters.Count).ThenBy(c => c.Position),
            _ => query.OrderBy(c => c.Book.Position).ThenBy(c => c.Position),
        };

        var total = await query.CountAsync();
        var data = await ordered
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new ChapterListItem(c.Id, c.Title, c.Position, c.BookId, c.Book.Title, c.SubChapters.Count))
            .ToListAsync();

        return new PagedResult<ChapterListItem>(data, total, page, pageSize);
    }

    public async Task<PagedResult<SubChapterListItem>> GetSubChaptersAsync(int page, int pageSize, Guid? bookId, string? search, string? sort)
    {
        var query = db.SubChapters.Include(s => s.Chapter).ThenInclude(c => c.Book).AsQueryable();
        if (bookId.HasValue) query = query.Where(s => s.Chapter.BookId == bookId.Value);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(s => s.Title.Contains(search));

        // Default (no/unknown sort) groups subchapters under their chapter, then orders within it.
        var ordered = sort switch
        {
            "position_desc" => query.OrderByDescending(s => s.Position),
            "position_asc" => query.OrderBy(s => s.Position),
            "title_asc" => query.OrderBy(s => s.Title),
            "title_desc" => query.OrderByDescending(s => s.Title),
            "chapter_asc" => query.OrderBy(s => s.Chapter.Title).ThenBy(s => s.Position),
            "chapter_desc" => query.OrderByDescending(s => s.Chapter.Title).ThenBy(s => s.Position),
            "book_asc" => query.OrderBy(s => s.Chapter.Book.Title).ThenBy(s => s.Position),
            "book_desc" => query.OrderByDescending(s => s.Chapter.Book.Title).ThenBy(s => s.Position),
            _ => query.OrderBy(s => s.Chapter.Position).ThenBy(s => s.Position),
        };

        var total = await query.CountAsync();
        var data = await ordered
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new SubChapterListItem(s.Id, s.Title, s.Position, s.ChapterId, s.Chapter.Title, s.Chapter.BookId, s.Chapter.Book.Title, s.ParentSubChapterId))
            .ToListAsync();

        return new PagedResult<SubChapterListItem>(data, total, page, pageSize);
    }

    public async Task<IEnumerable<ChapterResponse>> GetChaptersByBookAsync(Guid bookId)
    {
        var chapters = await db.Chapters
            .Include(c => c.SubChapters)
            .Where(c => c.BookId == bookId)
            .OrderBy(c => c.Position)
            .ToListAsync();
        return chapters.Select(Mappers.ToChapterResponse);
    }

    public async Task<ChapterDetail?> GetChapterByIdAsync(Guid id)
    {
        var chapter = await db.Chapters
            .Include(c => c.Book)
            .Include(c => c.SubChapters)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (chapter is null) return null;
        return new ChapterDetail(
            chapter.Id, chapter.Title, chapter.Body, chapter.Position,
            chapter.BookId, chapter.Book.Title,
            chapter.SubChapters.OrderBy(s => s.Position).Select(Mappers.ToSubChapterResponse).ToList());
    }

    public async Task<SubChapterDetail?> GetSubChapterByIdAsync(Guid id)
    {
        var sub = await db.SubChapters
            .Include(s => s.Chapter).ThenInclude(c => c.Book)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (sub is null) return null;
        return new SubChapterDetail(
            sub.Id, sub.Title, sub.Body, sub.Position,
            sub.ChapterId, sub.Chapter.Title, sub.Chapter.BookId, sub.Chapter.Book.Title,
            sub.ParentSubChapterId);
    }

    public async Task<(ChapterResponse? Chapter, bool BookNotFound)> CreateChapterAsync(Guid bookId, SaveChapterRequest req)
    {
        var book = await db.Books.FindAsync(bookId);
        if (book is null) return (null, true);

        var position = req.Position ?? (await db.Chapters
            .Where(c => c.BookId == bookId)
            .MaxAsync(c => (int?)c.Position) ?? 0) + 1;

        var chapter = new Chapter
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            Title = req.Title,
            Body = req.Body,
            Position = position,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Chapters.Add(chapter);
        await db.SaveChangesAsync();
        chapter.SubChapters = [];
        return (Mappers.ToChapterResponse(chapter), false);
    }

    public async Task<ChapterResponse?> UpdateChapterAsync(Guid id, SaveChapterRequest req)
    {
        var chapter = await db.Chapters.Include(c => c.SubChapters).FirstOrDefaultAsync(c => c.Id == id);
        if (chapter is null) return null;

        chapter.Title = req.Title;
        chapter.Body = req.Body;
        if (req.Position.HasValue) chapter.Position = req.Position.Value;
        chapter.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Mappers.ToChapterResponse(chapter);
    }

    public async Task<bool> DeleteChapterAsync(Guid id)
    {
        var chapter = await db.Chapters.FindAsync(id);
        if (chapter is null) return false;
        db.Chapters.Remove(chapter);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<(SubChapterResponse? Sub, bool ChapterNotFound)> CreateSubChapterAsync(CreateSubChapterRequest req)
    {
        var chapter = await db.Chapters.FindAsync(req.ChapterId);
        if (chapter is null) return (null, true);

        var position = req.Position ?? (await db.SubChapters
            .Where(s => s.ChapterId == req.ChapterId && s.ParentSubChapterId == req.ParentSubChapterId)
            .MaxAsync(s => (int?)s.Position) ?? 0) + 1;

        var sub = new SubChapter
        {
            Id = Guid.NewGuid(),
            ChapterId = req.ChapterId,
            ParentSubChapterId = req.ParentSubChapterId,
            Title = req.Title,
            Body = req.Body,
            Position = position,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.SubChapters.Add(sub);
        await db.SaveChangesAsync();
        return (Mappers.ToSubChapterResponse(sub), false);
    }

    public async Task<(SubChapterResponse? Sub, bool ChapterNotFound)> CreateSubChapterUnderChapterAsync(Guid chapterId, SaveSubChapterRequest req)
    {
        var chapter = await db.Chapters.FindAsync(chapterId);
        if (chapter is null) return (null, true);

        var position = req.Position ?? (await db.SubChapters
            .Where(s => s.ChapterId == chapterId && s.ParentSubChapterId == null)
            .MaxAsync(s => (int?)s.Position) ?? 0) + 1;

        var sub = new SubChapter
        {
            Id = Guid.NewGuid(),
            ChapterId = chapterId,
            ParentSubChapterId = req.ParentSubChapterId,
            Title = req.Title,
            Body = req.Body,
            Position = position,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.SubChapters.Add(sub);
        await db.SaveChangesAsync();
        return (Mappers.ToSubChapterResponse(sub), false);
    }

    public async Task<SubChapterResponse?> UpdateSubChapterAsync(Guid id, SaveSubChapterRequest req)
    {
        var sub = await db.SubChapters.FindAsync(id);
        if (sub is null) return null;

        sub.Title = req.Title;
        sub.Body = req.Body;
        if (req.Position.HasValue) sub.Position = req.Position.Value;
        if (req.ChapterId.HasValue) sub.ChapterId = req.ChapterId.Value;
        sub.ParentSubChapterId = req.ParentSubChapterId;
        sub.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Mappers.ToSubChapterResponse(sub);
    }

    public async Task<bool> DeleteSubChapterAsync(Guid id)
    {
        var sub = await db.SubChapters.FindAsync(id);
        if (sub is null) return false;
        db.SubChapters.Remove(sub);
        await db.SaveChangesAsync();
        return true;
    }
}
