using IslamiJindegiApi.Data;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IslamiJindegiApi.Commands;

/// <summary>One-time, repeatable import from the retired Django Hijri service.</summary>
public static class MigrateHijriSightingsCommand
{
    public static async Task RunAsync(string connectionString, AppDbContext db)
    {
        await using var source = new NpgsqlConnection(ToNpgsqlConnectionString(connectionString));
        await source.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT country_code, hijri_year, hijri_month, gregorian_start_date, created_at, updated_at
            FROM sightings_hijrimonthsighting
            """, source);
        await using var reader = await command.ExecuteReaderAsync();

        var rows = new List<(string CountryCode, int Year, int Month, DateOnly Start, DateTime Created, DateTime Updated)>();
        while (await reader.ReadAsync())
        {
            rows.Add((
                reader.GetString(0).ToUpperInvariant(),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetFieldValue<DateOnly>(3),
                DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
                DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)));
        }

        var existing = await db.HijriMonthSightings
            .ToDictionaryAsync(x => (x.CountryCode, x.HijriYear, x.HijriMonth));

        foreach (var row in rows)
        {
            if (!existing.TryGetValue((row.CountryCode, row.Year, row.Month), out var sighting))
            {
                sighting = new HijriMonthSighting { Id = Guid.NewGuid() };
                db.HijriMonthSightings.Add(sighting);
            }

            sighting.CountryCode = row.CountryCode;
            sighting.HijriYear = row.Year;
            sighting.HijriMonth = row.Month;
            sighting.GregorianStartDate = row.Start;
            sighting.CreatedAt = row.Created;
            sighting.UpdatedAt = row.Updated;
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"Imported {rows.Count} Hijri sightings from Django.");
    }

    static string ToNpgsqlConnectionString(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return value;

        var uri = new Uri(value.Replace("postgres://", "http://", StringComparison.OrdinalIgnoreCase)
            .Replace("postgresql://", "http://", StringComparison.OrdinalIgnoreCase));
        var credentials = uri.UserInfo.Split(':', 2);
        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = credentials[0],
            Password = credentials.Length > 1 ? credentials[1] : string.Empty,
            SslMode = SslMode.Require,
        }.ConnectionString;
    }
}
