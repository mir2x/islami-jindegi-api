using System.Text.Json;
using System.Text.Json.Serialization;
using IslamiJindegiApi.Data;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Commands;

public static class ImportArabicPlainTextCommand
{
    record PlainTextRow(
        [property: JsonPropertyName("sura")] int Sura,
        [property: JsonPropertyName("ayah")] int Ayah,
        [property: JsonPropertyName("arabic_text_plain")] string? ArabicTextPlain);

    const int BatchSize = 1000;

    public static async Task RunAsync(AppDbContext db, string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"  File not found: {jsonPath}");
            return;
        }

        var ayahsByKey = await db.QuranAyahs.ToDictionaryAsync(a => (a.SurahNumber, a.AyahNumber));

        var updated = 0;
        var pending = 0;

        await using var stream = File.OpenRead(jsonPath);
        await foreach (var row in JsonSerializer.DeserializeAsyncEnumerable<PlainTextRow>(stream))
        {
            if (row is null || row.ArabicTextPlain is null) continue;
            if (!ayahsByKey.TryGetValue((row.Sura, row.Ayah), out var ayah))
            {
                Console.WriteLine($"  Warning: no quran_ayahs row for {row.Sura}:{row.Ayah}, skipping");
                continue;
            }

            ayah.ArabicTextPlain = row.ArabicTextPlain;
            updated++;
            pending++;

            if (pending >= BatchSize)
            {
                await db.SaveChangesAsync();
                pending = 0;
            }
        }

        if (pending > 0)
            await db.SaveChangesAsync();

        Console.WriteLine($"\nArabic plain-text import complete. {updated} ayahs updated.");
    }
}
