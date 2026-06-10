using IslamiJindegiApi.Data;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Commands;

public static class SeedBd1447Command
{
    // Bangladesh verified moon-sighting dates for Hijri year 1447
    // Month 12 (Dhul Hijja) omitted — add when announced
    static readonly (int Month, DateOnly Start)[] Data =
    [
        (1,  new DateOnly(2025, 6,  27)),  // Muharram
        (2,  new DateOnly(2025, 7,  27)),  // Safar
        (3,  new DateOnly(2025, 8,  25)),  // Rabi al-Awwal
        (4,  new DateOnly(2025, 9,  24)),  // Rabi al-Thani
        (5,  new DateOnly(2025, 10, 24)),  // Jumada al-Ula
        (6,  new DateOnly(2025, 11, 23)),  // Jumada al-Thani
        (7,  new DateOnly(2025, 12, 22)),  // Rajab
        (8,  new DateOnly(2026, 1,  21)),  // Sha'ban
        (9,  new DateOnly(2026, 2,  19)),  // Ramadan
        (10, new DateOnly(2026, 3,  21)),  // Shawwal
        (11, new DateOnly(2026, 4,  20)),  // Dhu al-Qi'dah
        // (12, ???),                       // Dhu al-Hijjah — add when announced
    ];

    public static async Task RunAsync(AppDbContext db)
    {
        int created = 0, skipped = 0;

        foreach (var (month, start) in Data)
        {
            var exists = await db.HijriMonthSightings.AnyAsync(s =>
                s.CountryCode == "BD" &&
                s.HijriYear   == 1447 &&
                s.HijriMonth  == month);

            if (exists)
            {
                Console.WriteLine($"  Skipped  BD 1447/{month:D2} — already exists");
                skipped++;
            }
            else
            {
                db.HijriMonthSightings.Add(new HijriMonthSighting
                {
                    Id                 = Guid.NewGuid(),
                    CountryCode        = "BD",
                    HijriYear          = 1447,
                    HijriMonth         = month,
                    GregorianStartDate = start,
                    CreatedAt          = DateTime.UtcNow,
                    UpdatedAt          = DateTime.UtcNow,
                });
                Console.WriteLine($"  Created  BD 1447/{month:D2} → {start:yyyy-MM-dd}");
                created++;
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"\nDone. {created} created, {skipped} skipped.");
    }
}
