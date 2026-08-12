using IslamiJindegiApi.Data;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Commands;

// One-off undo for BackfillOfflineAvailabilityCommand's book pass: the
// legacy offline_data/books.sqlite3 dump turned out to be the entire old
// catalog (no real per-item curation existed pre-migration), so matching by
// title against it just flipped IsOfflineAvailable on nearly every book
// instead of a deliberately curated subset. Reverts every book currently
// marked offline-available back to false so curation can be done by hand
// from the admin panel going forward.
public static class ResetBooksOfflineAvailabilityCommand
{
    public static async Task RunAsync(AppDbContext db)
    {
        var books = await db.Books.Where(b => b.IsOfflineAvailable).ToListAsync();
        foreach (var book in books)
            book.IsOfflineAvailable = false;

        await db.SaveChangesAsync();
        Console.WriteLine($"Reset {books.Count} book(s) to IsOfflineAvailable = false.");
    }
}
