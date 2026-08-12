using IslamiJindegiApi.Data;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Commands;

// One-off blanket reset of IsOfflineAvailable across all seven content
// types, following the finding that the legacy offline_data dumps used by
// BackfillOfflineAvailabilityCommand had no real per-item curation signal
// (they were just the entire old catalog). Bayan and Book are large/heavy
// (audio and nested chapter bodies) and go fully offline-unavailable so they
// can be curated by hand from the admin panel; the remaining five content
// types (which are small/light enough that a full sync isn't a problem) go
// fully offline-available.
public static class SetOfflineAvailabilityDefaultsCommand
{
    public static async Task RunAsync(AppDbContext db)
    {
        var bayans = await db.Bayans.ExecuteUpdateAsync(s => s.SetProperty(b => b.IsOfflineAvailable, false));
        var books = await db.Books.ExecuteUpdateAsync(s => s.SetProperty(b => b.IsOfflineAvailable, false));
        var articles = await db.Articles.ExecuteUpdateAsync(s => s.SetProperty(a => a.IsOfflineAvailable, true));
        var duas = await db.Duas.ExecuteUpdateAsync(s => s.SetProperty(d => d.IsOfflineAvailable, true));
        var madrasahs = await db.Madrasahs.ExecuteUpdateAsync(s => s.SetProperty(m => m.IsOfflineAvailable, true));
        var malfuzats = await db.Malfuzats.ExecuteUpdateAsync(s => s.SetProperty(m => m.IsOfflineAvailable, true));
        var masails = await db.Masails.ExecuteUpdateAsync(s => s.SetProperty(m => m.IsOfflineAvailable, true));

        Console.WriteLine($"Bayans:    {bayans} set to false.");
        Console.WriteLine($"Books:     {books} set to false.");
        Console.WriteLine($"Articles:  {articles} set to true.");
        Console.WriteLine($"Duas:      {duas} set to true.");
        Console.WriteLine($"Madrasahs: {madrasahs} set to true.");
        Console.WriteLine($"Malfuzats: {malfuzats} set to true.");
        Console.WriteLine($"Masails:   {masails} set to true.");
    }
}
