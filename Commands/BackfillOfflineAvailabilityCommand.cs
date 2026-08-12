using IslamiJindegiApi.Data;
using IslamiJindegiApi.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Commands;

// Reconciles a set of legacy per-module SQLite dumps (offline_data/*.sqlite3 —
// exported from an older offline-sync generation, so their ids no longer match
// the current Postgres rows) against the live tables by title and flips
// IsOfflineAvailable = true on every unambiguous match. Anything that can't
// be matched confidently is written to a report file for manual review
// instead of being guessed at.
public static class BackfillOfflineAvailabilityCommand
{
    public static async Task RunAsync(AppDbContext db, string dataDir)
    {
        var reportsDir = Path.Combine(dataDir, "reports");
        Directory.CreateDirectory(reportsDir);

        await BackfillBayans(db, dataDir, reportsDir);
        await BackfillArticles(db, dataDir, reportsDir);
        await BackfillBooks(db, dataDir, reportsDir);
        await BackfillDuas(db, dataDir, reportsDir);
        await BackfillMadrasahs(db, dataDir, reportsDir);
        await BackfillMalfuzats(db, dataDir, reportsDir);
        await BackfillMasails(db, dataDir, reportsDir);

        Console.WriteLine("\nBackfill complete. Flagged items (if any) were written to offline_data/reports/.");
    }

    static Task BackfillBayans(AppDbContext db, string dataDir, string reportsDir) =>
        Backfill(db, dataDir, reportsDir, "bayans", "Bayan",
            db.Bayans, b => b.Title, b => b.IsOfflineAvailable,
            b => b.IsOfflineAvailable = true);

    static Task BackfillArticles(AppDbContext db, string dataDir, string reportsDir) =>
        Backfill(db, dataDir, reportsDir, "articles", "Article",
            db.Articles, a => a.Title, a => a.IsOfflineAvailable,
            a => a.IsOfflineAvailable = true);

    static Task BackfillBooks(AppDbContext db, string dataDir, string reportsDir) =>
        Backfill(db, dataDir, reportsDir, "books", "Book",
            db.Books, b => b.Title, b => b.IsOfflineAvailable,
            b => b.IsOfflineAvailable = true);

    static Task BackfillDuas(AppDbContext db, string dataDir, string reportsDir) =>
        Backfill(db, dataDir, reportsDir, "duas", "Dua",
            db.Duas, d => d.Title, d => d.IsOfflineAvailable,
            d => d.IsOfflineAvailable = true);

    static Task BackfillMadrasahs(AppDbContext db, string dataDir, string reportsDir) =>
        Backfill(db, dataDir, reportsDir, "madrasahs", "Madrasah",
            db.Madrasahs, m => m.Title, m => m.IsOfflineAvailable,
            m => m.IsOfflineAvailable = true);

    static Task BackfillMalfuzats(AppDbContext db, string dataDir, string reportsDir) =>
        Backfill(db, dataDir, reportsDir, "malfuzats", "Malfuzat",
            db.Malfuzats, m => m.Title, m => m.IsOfflineAvailable,
            m => m.IsOfflineAvailable = true);

    static Task BackfillMasails(AppDbContext db, string dataDir, string reportsDir) =>
        Backfill(db, dataDir, reportsDir, "masails", "Masail",
            db.Masails, m => m.Title, m => m.IsOfflineAvailable,
            m => m.IsOfflineAvailable = true);

    // ── Generic reconciliation ───────────────────────────────────────────────

    static async Task Backfill<T>(
        AppDbContext db,
        string dataDir,
        string reportsDir,
        string sqliteFileName,
        string label,
        DbSet<T> dbSet,
        Func<T, string> getTitle,
        Func<T, bool> getOffline,
        Action<T> markOffline) where T : class
    {
        var sqlitePath = Path.Combine(dataDir, $"{sqliteFileName}.sqlite3");
        if (!File.Exists(sqlitePath))
        {
            Console.WriteLine($"{label}: {sqlitePath} not found, skipped.");
            return;
        }

        Console.Write($"{label}... ");

        var legacyRows = ReadLegacyRows(sqlitePath, sqliteFileName);

        var current = await dbSet.ToListAsync();
        var byTitle = current
            .GroupBy(e => getTitle(e).Trim())
            .ToDictionary(g => g.Key, g => g.ToList());

        var flagged = new List<(string LegacyId, string Title, int? Position, string Reason)>();
        int alreadyOffline = 0, newlyMatched = 0;

        foreach (var row in legacyRows)
        {
            var key = row.Title.Trim();
            if (!byTitle.TryGetValue(key, out var matches))
            {
                flagged.Add((row.Id, row.Title, row.Position, "no title match in new system"));
                continue;
            }

            if (matches.Count > 1)
            {
                flagged.Add((row.Id, row.Title, row.Position, $"ambiguous — {matches.Count} new-system rows share this title"));
                continue;
            }

            var match = matches[0];
            if (getOffline(match))
            {
                alreadyOffline++;
            }
            else
            {
                markOffline(match);
                newlyMatched++;
            }
        }

        await db.SaveChangesAsync();

        Console.WriteLine(
            $"{legacyRows.Count} legacy rows -> {newlyMatched} newly marked offline-available, " +
            $"{alreadyOffline} already were, {flagged.Count} flagged.");

        if (flagged.Count > 0)
            WriteReport(reportsDir, sqliteFileName, flagged);
    }

    static List<(string Id, string Title, int? Position)> ReadLegacyRows(string sqlitePath, string table)
    {
        using var conn = new SqliteConnection($"Data Source={sqlitePath};Mode=ReadOnly");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT id, title, position FROM {table}";

        var rows = new List<(string, string, int?)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetString(0);
            var title = reader.IsDBNull(1) ? "" : reader.GetString(1);
            int? position = reader.IsDBNull(2) ? null : reader.GetInt32(2);
            rows.Add((id, title, position));
        }
        return rows;
    }

    static void WriteReport(
        string reportsDir, string sqliteFileName,
        List<(string LegacyId, string Title, int? Position, string Reason)> flagged)
    {
        var path = Path.Combine(reportsDir, $"{sqliteFileName}_flagged.csv");
        using var writer = new StreamWriter(path, append: false);
        writer.WriteLine("legacy_id,title,position,reason");
        foreach (var f in flagged)
        {
            var title = f.Title.Replace("\"", "\"\"");
            writer.WriteLine($"{f.LegacyId},\"{title}\",{f.Position},\"{f.Reason}\"");
        }
    }
}
