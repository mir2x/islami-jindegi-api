using IslamiJindegiApi.Data;
using IslamiJindegiApi.Services;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Commands;

/// <summary>One-off, idempotent backfill for book navigation reading order.</summary>
public static class RecomputeReadingOrderCommand
{
    public static async Task RunAsync(AppDbContext db, IChapterService chapterService)
    {
        var bookIds = await db.Books.Select(b => b.Id).ToListAsync();
        foreach (var bookId in bookIds)
            await chapterService.RecomputeReadingOrderAsync(bookId);
        Console.WriteLine($"Recomputed reading order for {bookIds.Count} book(s).");
    }
}
