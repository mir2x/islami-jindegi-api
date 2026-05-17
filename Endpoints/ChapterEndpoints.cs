using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class ChapterEndpoints
{
    public static void MapChapterEndpoints(this WebApplication app)
    {
        app.MapGet("/api/books/{bookId:guid}/chapters", async (Guid bookId, AppDbContext db) =>
        {
            var chapters = await db.Chapters
                .Include(c => c.SubChapters)
                .Where(c => c.BookId == bookId)
                .OrderBy(c => c.Position)
                .ToListAsync();

            return Results.Ok(chapters.Select(ToResponse));
        });

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
            return Results.Created($"/api/chapters/{chapter.Id}", ToResponse(chapter));
        });

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

        app.MapDelete("/api/chapters/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var chapter = await db.Chapters.FindAsync(id);
            if (chapter is null) return Results.NotFound();

            db.Chapters.Remove(chapter);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapPost("/api/chapters/{chapterId:guid}/subchapters", async (Guid chapterId, SaveSubChapterRequest req, AppDbContext db) =>
        {
            var chapter = await db.Chapters.FindAsync(chapterId);
            if (chapter is null) return Results.NotFound();

            var position = req.Position ?? (await db.SubChapters
                .Where(s => s.ChapterId == chapterId)
                .MaxAsync(s => (int?)s.Position) ?? 0) + 1;

            var subChapter = new SubChapter
            {
                Id = Guid.NewGuid(),
                ChapterId = chapterId,
                Title = req.Title,
                Body = req.Body,
                Position = position,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.SubChapters.Add(subChapter);
            await db.SaveChangesAsync();
            return Results.Created($"/api/subchapters/{subChapter.Id}", new SubChapterResponse(subChapter.Id, subChapter.Title, subChapter.Body, subChapter.Position));
        });

        app.MapPut("/api/subchapters/{id:guid}", async (Guid id, SaveSubChapterRequest req, AppDbContext db) =>
        {
            var subChapter = await db.SubChapters.FindAsync(id);
            if (subChapter is null) return Results.NotFound();

            subChapter.Title = req.Title;
            subChapter.Body = req.Body;
            if (req.Position.HasValue) subChapter.Position = req.Position.Value;
            subChapter.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new SubChapterResponse(subChapter.Id, subChapter.Title, subChapter.Body, subChapter.Position));
        });

        app.MapDelete("/api/subchapters/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var subChapter = await db.SubChapters.FindAsync(id);
            if (subChapter is null) return Results.NotFound();

            db.SubChapters.Remove(subChapter);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    public static ChapterResponse ToResponse(Chapter c) => new(
        c.Id, c.Title, c.Body, c.Position,
        c.SubChapters.OrderBy(s => s.Position)
            .Select(s => new SubChapterResponse(s.Id, s.Title, s.Body, s.Position))
            .ToList());
}
