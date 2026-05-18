using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class ChapterEndpoints
{
    public static void MapChapterEndpoints(this WebApplication app)
    {
        // Flat paginated list of chapters
        app.MapGet("/api/chapters", async (AppDbContext db, Guid? bookId, string? search, int page = 1, int pageSize = 20) =>
        {
            var query = db.Chapters.Include(c => c.Book).Include(c => c.SubChapters).AsQueryable();
            if (bookId.HasValue) query = query.Where(c => c.BookId == bookId.Value);
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.Title.Contains(search));

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(c => c.Book.Position).ThenBy(c => c.Position)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new ChapterListItem(c.Id, c.Title, c.Position, c.BookId, c.Book.Title, c.SubChapters.Count))
                .ToListAsync();

            return Results.Ok(new PagedResult<ChapterListItem>(data, total, page, pageSize));
        });

        // Flat paginated list of subchapters
        app.MapGet("/api/subchapters", async (AppDbContext db, Guid? bookId, string? search, int page = 1, int pageSize = 20) =>
        {
            var query = db.SubChapters.Include(s => s.Chapter).ThenInclude(c => c.Book).AsQueryable();
            if (bookId.HasValue) query = query.Where(s => s.Chapter.BookId == bookId.Value);
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(s => s.Title.Contains(search));

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(s => s.Chapter.Position).ThenBy(s => s.Position)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(s => new SubChapterListItem(s.Id, s.Title, s.Position, s.ChapterId, s.Chapter.Title, s.Chapter.BookId, s.Chapter.Book.Title, s.ParentSubChapterId))
                .ToListAsync();

            return Results.Ok(new PagedResult<SubChapterListItem>(data, total, page, pageSize));
        });

        // Chapters for a book (used in detail page + parent picker)
        app.MapGet("/api/books/{bookId:guid}/chapters", async (Guid bookId, AppDbContext db) =>
        {
            var chapters = await db.Chapters
                .Include(c => c.SubChapters)
                .Where(c => c.BookId == bookId)
                .OrderBy(c => c.Position)
                .ToListAsync();

            return Results.Ok(chapters.Select(ToResponse));
        });

        // Get single chapter (with body + book info for edit forms)
        app.MapGet("/api/chapters/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var chapter = await db.Chapters
                .Include(c => c.Book)
                .Include(c => c.SubChapters)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (chapter is null) return Results.NotFound();
            return Results.Ok(new ChapterDetail(
                chapter.Id, chapter.Title, chapter.Body, chapter.Position,
                chapter.BookId, chapter.Book.Title,
                chapter.SubChapters.OrderBy(s => s.Position).Select(ToSubResponse).ToList()));
        });

        // Get single subchapter (with body + chapter/book info for edit forms)
        app.MapGet("/api/subchapters/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var sub = await db.SubChapters
                .Include(s => s.Chapter).ThenInclude(c => c.Book)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sub is null) return Results.NotFound();
            return Results.Ok(new SubChapterDetail(
                sub.Id, sub.Title, sub.Body, sub.Position,
                sub.ChapterId, sub.Chapter.Title, sub.Chapter.BookId, sub.Chapter.Book.Title,
                sub.ParentSubChapterId));
        });

        // Create chapter under a book
        app.MapPost("/api/books/{bookId:guid}/chapters", async (Guid bookId, SaveChapterRequest req, AppDbContext db) =>
        {
            var book = await db.Books.FindAsync(bookId);
            if (book is null) return Results.NotFound();

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
            return Results.Created($"/api/chapters/{chapter.Id}", ToResponse(chapter));
        });

        // Update chapter
        app.MapPut("/api/chapters/{id:guid}", async (Guid id, SaveChapterRequest req, AppDbContext db) =>
        {
            var chapter = await db.Chapters.Include(c => c.SubChapters).FirstOrDefaultAsync(c => c.Id == id);
            if (chapter is null) return Results.NotFound();

            chapter.Title = req.Title;
            chapter.Body = req.Body;
            if (req.Position.HasValue) chapter.Position = req.Position.Value;
            chapter.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(chapter));
        });

        // Delete chapter
        app.MapDelete("/api/chapters/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var chapter = await db.Chapters.FindAsync(id);
            if (chapter is null) return Results.NotFound();
            db.Chapters.Remove(chapter);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Create subchapter (flat — accepts chapterId in body, optional parentSubChapterId)
        app.MapPost("/api/subchapters", async (CreateSubChapterRequest req, AppDbContext db) =>
        {
            var chapter = await db.Chapters.FindAsync(req.ChapterId);
            if (chapter is null) return Results.NotFound();

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
            return Results.Created($"/api/subchapters/{sub.Id}", ToSubResponse(sub));
        });

        // Keep legacy nested create for book detail quick-add
        app.MapPost("/api/chapters/{chapterId:guid}/subchapters", async (Guid chapterId, SaveSubChapterRequest req, AppDbContext db) =>
        {
            var chapter = await db.Chapters.FindAsync(chapterId);
            if (chapter is null) return Results.NotFound();

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
            return Results.Created($"/api/subchapters/{sub.Id}", ToSubResponse(sub));
        });

        // Update subchapter
        app.MapPut("/api/subchapters/{id:guid}", async (Guid id, SaveSubChapterRequest req, AppDbContext db) =>
        {
            var sub = await db.SubChapters.FindAsync(id);
            if (sub is null) return Results.NotFound();

            sub.Title = req.Title;
            sub.Body = req.Body;
            if (req.Position.HasValue) sub.Position = req.Position.Value;
            if (req.ParentSubChapterId.HasValue) sub.ParentSubChapterId = req.ParentSubChapterId;
            sub.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToSubResponse(sub));
        });

        // Delete subchapter
        app.MapDelete("/api/subchapters/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var sub = await db.SubChapters.FindAsync(id);
            if (sub is null) return Results.NotFound();
            db.SubChapters.Remove(sub);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    public static ChapterResponse ToResponse(Chapter c) => new(
        c.Id, c.Title, c.Body, c.Position,
        c.SubChapters.OrderBy(s => s.Position)
            .Select(ToSubResponse)
            .ToList());

    public static SubChapterResponse ToSubResponse(SubChapter s) =>
        new(s.Id, s.Title, s.Body, s.Position, s.ParentSubChapterId);
}
