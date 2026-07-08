using System.Text.Json;
using System.Text.Json.Serialization;
using IslamiJindegiApi.Data;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Commands;

public static class ImportTafsirCommand
{
    record TafsirRow(
        [property: JsonPropertyName("sura")] int Sura,
        [property: JsonPropertyName("ayah")] int Ayah,
        [property: JsonPropertyName("tafsir")] string Tafsir);

    // File name (without .json) doubles as the TafsirId — matches QuranService.ValidTafsirs.
    static readonly string[] Files =
    [
        "tafsir_taqi_usmani_bn.json",
        "tafsir_ibn_kathir_bn.json",
        "tafsir_ibn_kathir_en.json",
        "tafsir_maariful_quran_bn.json",
        "tafsir_maariful_quran_en.json",
    ];

    const int BatchSize = 1000;

    public static async Task RunAsync(AppDbContext db, string dataDir)
    {
        foreach (var file in Files)
        {
            var tafsirId = Path.GetFileNameWithoutExtension(file);
            var path = Path.Combine(dataDir, file);
            if (!File.Exists(path))
            {
                Console.WriteLine($"  Skipped  {tafsirId} — file not found at {path}");
                continue;
            }

            var deleted = await db.QuranTafsirs.Where(t => t.TafsirId == tafsirId).ExecuteDeleteAsync();
            if (deleted > 0)
                Console.WriteLine($"  Cleared  {deleted} existing rows for {tafsirId}");

            Console.WriteLine($"  Importing {tafsirId} from {path} ...");

            var count = 0;
            var batch = new List<QuranTafsir>(BatchSize);

            await using var stream = File.OpenRead(path);
            await foreach (var row in JsonSerializer.DeserializeAsyncEnumerable<TafsirRow>(stream))
            {
                if (row is null) continue;

                batch.Add(new QuranTafsir
                {
                    SurahNumber = row.Sura,
                    AyahNumber = row.Ayah,
                    TafsirId = tafsirId,
                    TafsirText = row.Tafsir,
                });

                if (batch.Count >= BatchSize)
                {
                    db.QuranTafsirs.AddRange(batch);
                    await db.SaveChangesAsync();
                    count += batch.Count;
                    batch.Clear();
                    db.ChangeTracker.Clear();
                }
            }

            if (batch.Count > 0)
            {
                db.QuranTafsirs.AddRange(batch);
                await db.SaveChangesAsync();
                count += batch.Count;
                db.ChangeTracker.Clear();
            }

            Console.WriteLine($"  Done     {tafsirId} — {count} rows imported");
        }

        Console.WriteLine("\nTafsir import complete.");
    }
}
