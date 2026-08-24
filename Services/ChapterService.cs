using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class ChapterService(AppDbContext db, ContentSyncNotifier syncNotifier) : IChapterService
{
    public async Task<PagedResult<ChapterListItem>> GetChaptersAsync(int page, int pageSize, Guid? bookId, string? search, string? sort)
    {
        var query = db.Chapters.AsNoTracking().Include(c => c.Book).Include(c => c.SubChapters).AsQueryable();
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
        var query = db.SubChapters.AsNoTracking().Include(s => s.Chapter).ThenInclude(c => c.Book).AsQueryable();
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

    public async Task<IEnumerable<ChapterResponse>> GetChaptersByBookAsync(Guid bookId, bool includeUnpublished = false)
    {
        var chapters = await db.Chapters
            .AsNoTracking()
            .Include(c => c.SubChapters)
            .Where(c => c.BookId == bookId && (includeUnpublished || c.Book.Published))
            .OrderBy(c => c.Position)
            .ToListAsync();
        return chapters.Select(Mappers.ToChapterResponse);
    }

    public async Task<ChapterDetail?> GetChapterByIdAsync(Guid id, bool includeUnpublished = false)
    {
        var chapter = await db.Chapters
            .AsNoTracking()
            .Include(c => c.Book)
            .Include(c => c.SubChapters)
            .FirstOrDefaultAsync(c => c.Id == id && (includeUnpublished || c.Book.Published));
        if (chapter is null) return null;
        var (previous, next) = chapter.ReadingOrder is int order
            ? await GetBookNodeSiblingsAsync(chapter.BookId, order)
            : (null, null);
        return new ChapterDetail(
            chapter.Id, chapter.Title, chapter.Body, chapter.Position,
            chapter.BookId, chapter.Book.Title,
            chapter.SubChapters.OrderBy(s => s.ReadingOrder).ThenBy(s => s.Position).Select(Mappers.ToSubChapterResponse).ToList(),
            chapter.ReadingOrder, previous, next);
    }

    public async Task<SubChapterDetail?> GetSubChapterByIdAsync(Guid id, bool includeUnpublished = false)
    {
        var sub = await db.SubChapters
            .AsNoTracking()
            .Include(s => s.Chapter).ThenInclude(c => c.Book)
            .FirstOrDefaultAsync(s => s.Id == id && (includeUnpublished || s.Chapter.Book.Published));
        if (sub is null) return null;
        var (previous, next) = await GetBookNodeSiblingsAsync(sub.Chapter.BookId, sub.ReadingOrder);
        return new SubChapterDetail(
            sub.Id, sub.Title, sub.Body, sub.Position,
            sub.ChapterId, sub.Chapter.Title, sub.Chapter.BookId, sub.Chapter.Book.Title,
            sub.ParentSubChapterId, sub.ReadingOrder, previous, next);
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
        await RecomputeReadingOrderAsync(bookId);
        chapter.SubChapters = [];
        if (book.IsOfflineAvailable) await syncNotifier.NotifyAsync("books");
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
        await RecomputeReadingOrderAsync(chapter.BookId);
        if (await IsBookOfflineAvailableAsync(chapter.BookId)) await syncNotifier.NotifyAsync("books");
        return Mappers.ToChapterResponse(chapter);
    }

    public async Task<bool> DeleteChapterAsync(Guid id)
    {
        var chapter = await db.Chapters.FindAsync(id);
        if (chapter is null) return false;
        var wasBookOfflineAvailable = await IsBookOfflineAvailableAsync(chapter.BookId);
        db.Chapters.Remove(chapter);
        await db.SaveChangesAsync();
        await RecomputeReadingOrderAsync(chapter.BookId);
        if (wasBookOfflineAvailable) await syncNotifier.NotifyAsync("books");
        return true;
    }

    // Chapters/SubChapters have their own controller endpoints independent of
    // BooksController, so a chapter-only edit never touches the parent Book row —
    // these helpers look up the owning book's offline flag to decide whether an
    // edit here is worth notifying the app about.
    async Task<bool> IsBookOfflineAvailableAsync(Guid bookId)
        => await db.Books.Where(b => b.Id == bookId).Select(b => b.IsOfflineAvailable).FirstOrDefaultAsync();

    async Task<bool> IsBookOfflineAvailableViaChapterAsync(Guid chapterId)
        => await db.Chapters.Where(c => c.Id == chapterId).Select(c => c.Book.IsOfflineAvailable).FirstOrDefaultAsync();

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
        await RecomputeReadingOrderAsync(chapter.BookId);
        if (await IsBookOfflineAvailableAsync(chapter.BookId)) await syncNotifier.NotifyAsync("books");
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
        await RecomputeReadingOrderAsync(chapter.BookId);
        if (await IsBookOfflineAvailableAsync(chapter.BookId)) await syncNotifier.NotifyAsync("books");
        return (Mappers.ToSubChapterResponse(sub), false);
    }

    public async Task<SubChapterResponse?> UpdateSubChapterAsync(Guid id, SaveSubChapterRequest req)
    {
        var sub = await db.SubChapters.FindAsync(id);
        if (sub is null) return null;

        var oldBookId = await db.Chapters.Where(c => c.Id == sub.ChapterId).Select(c => c.BookId).FirstOrDefaultAsync();

        sub.Title = req.Title;
        sub.Body = req.Body;
        if (req.Position.HasValue) sub.Position = req.Position.Value;
        if (req.ChapterId.HasValue) sub.ChapterId = req.ChapterId.Value;
        sub.ParentSubChapterId = req.ParentSubChapterId;
        sub.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await RecomputeReadingOrderAsync(oldBookId);
        var newBookId = await db.Chapters.Where(c => c.Id == sub.ChapterId).Select(c => c.BookId).FirstOrDefaultAsync();
        if (newBookId != oldBookId) await RecomputeReadingOrderAsync(newBookId);
        if (await IsBookOfflineAvailableViaChapterAsync(sub.ChapterId)) await syncNotifier.NotifyAsync("books");
        return Mappers.ToSubChapterResponse(sub);
    }

    public async Task<bool> DeleteSubChapterAsync(Guid id)
    {
        var sub = await db.SubChapters.FindAsync(id);
        if (sub is null) return false;
        var wasBookOfflineAvailable = await IsBookOfflineAvailableViaChapterAsync(sub.ChapterId);
        db.SubChapters.Remove(sub);
        await db.SaveChangesAsync();
        var bookId = await db.Chapters.Where(c => c.Id == sub.ChapterId).Select(c => c.BookId).FirstOrDefaultAsync();
        await RecomputeReadingOrderAsync(bookId);
        if (wasBookOfflineAvailable) await syncNotifier.NotifyAsync("books");
        return true;
    }

    /// <summary>Keeps the book's navigable depth-first sequence dense.</summary>
    public async Task RecomputeReadingOrderAsync(Guid bookId)
    {
        var chapters = await db.Chapters
            .Where(c => c.BookId == bookId)
            .Include(c => c.SubChapters)
            .OrderBy(c => c.Position).ThenBy(c => c.Id)
            .ToListAsync();
        var order = 0;
        foreach (var chapter in chapters)
        {
            if (chapter.SubChapters.Count == 0)
            {
                chapter.ReadingOrder = order++;
                continue;
            }

            chapter.ReadingOrder = null;
            var roots = chapter.SubChapters.Where(s => s.ParentSubChapterId is null)
                .OrderBy(s => s.Position).ThenBy(s => s.Id).ToList();
            var children = chapter.SubChapters.Where(s => s.ParentSubChapterId is not null)
                .GroupBy(s => s.ParentSubChapterId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Position).ThenBy(s => s.Id).ToList());
            var visited = new HashSet<Guid>();
            void Walk(IEnumerable<SubChapter> nodes)
            {
                foreach (var node in nodes)
                {
                    if (!visited.Add(node.Id)) continue;
                    node.ReadingOrder = order++;
                    if (children.TryGetValue(node.Id, out var descendants)) Walk(descendants);
                }
            }
            Walk(roots);
            // Legacy data has no database constraint that a parent belongs to
            // this chapter. Preserve every node in the sequence even when its
            // parent is deleted, cross-chapter, or cyclic.
            Walk(chapter.SubChapters
                .Where(s => !visited.Contains(s.Id))
                .OrderBy(s => s.Position).ThenBy(s => s.Id));
        }
        await db.SaveChangesAsync();
    }

    async Task<(BookNodeRef? Previous, BookNodeRef? Next)> GetBookNodeSiblingsAsync(Guid bookId, int order)
    {
        async Task<(BookNodeRef? Chapter, BookNodeRef? Sub)> CandidatesAsync(bool next)
        {
            var chapterQuery = db.Chapters.AsNoTracking()
                .Where(c => c.BookId == bookId && c.ReadingOrder != null
                    && (next ? c.ReadingOrder > order : c.ReadingOrder < order));
            var subQuery = db.SubChapters.AsNoTracking()
                .Where(s => s.Chapter.BookId == bookId
                    && (next ? s.ReadingOrder > order : s.ReadingOrder < order));
            var chapter = next
                ? await chapterQuery.OrderBy(c => c.ReadingOrder).Select(c => new BookNodeRef(c.Id, c.Title, c.ReadingOrder!.Value, "chapter")).FirstOrDefaultAsync()
                : await chapterQuery.OrderByDescending(c => c.ReadingOrder).Select(c => new BookNodeRef(c.Id, c.Title, c.ReadingOrder!.Value, "chapter")).FirstOrDefaultAsync();
            var sub = next
                ? await subQuery.OrderBy(s => s.ReadingOrder).Select(s => new BookNodeRef(s.Id, s.Title, s.ReadingOrder, "subchapter")).FirstOrDefaultAsync()
                : await subQuery.OrderByDescending(s => s.ReadingOrder).Select(s => new BookNodeRef(s.Id, s.Title, s.ReadingOrder, "subchapter")).FirstOrDefaultAsync();
            return (chapter, sub);
        }

        var (previousChapter, previousSub) = await CandidatesAsync(next: false);
        var (nextChapter, nextSub) = await CandidatesAsync(next: true);
        BookNodeRef? Choose(BookNodeRef? chapter, BookNodeRef? sub, bool isNext) => chapter is null ? sub
            : sub is null ? chapter
            : (isNext ? chapter.ReadingOrder < sub.ReadingOrder : chapter.ReadingOrder > sub.ReadingOrder) ? chapter : sub;
        return (Choose(previousChapter, previousSub, false), Choose(nextChapter, nextSub, true));
    }
}
