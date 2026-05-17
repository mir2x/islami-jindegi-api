using System.Text.Json;
using IslamiJindegiApi.Data;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IslamiJindegiApi.Commands;

public static class MigrateDataCommand
{
    const string TigrisBase = "https://static.islamijindegi.com/uploads/store/";

    public static async Task RunAsync(string oldConnStr, AppDbContext newDb)
    {
        await using var old = new NpgsqlConnection(oldConnStr);
        await old.OpenAsync();

        // Enforce read-only at session level — cannot write to old DB even by accident
        await Exec(old, "SET SESSION CHARACTERISTICS AS TRANSACTION READ ONLY");

        Console.WriteLine("Connected to old DB (read-only). Starting migration...\n");

        await MigrateAuthors(old, newDb);
        await MigrateCategories(old, newDb);
        await MigrateBooks(old, newDb);
        await MigrateBookAuthors(old, newDb);
        await MigrateBookCategories(old, newDb);
        await MigrateChapters(old, newDb);
        await MigrateSubChapters(old, newDb);

        Console.WriteLine("\nMigration complete.");
    }

    static async Task MigrateAuthors(NpgsqlConnection old, AppDbContext db)
    {
        Console.Write("Authors... ");
        var rows = await Query(old,
            "SELECT id, name, info, position, created_at, updated_at FROM authors ORDER BY position");

        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var existing = await db.Authors.FindAsync(id);
            if (existing is null)
                db.Authors.Add(new Author
                {
                    Id = id,
                    Name = (string)r[1]!,
                    Info = r[2] as string,
                    Position = (int)r[3]!,
                    CreatedAt = Utc(r[4]),
                    UpdatedAt = Utc(r[5])
                });
            else
            {
                existing.Name = (string)r[1]!;
                existing.Info = r[2] as string;
                existing.Position = (int)r[3]!;
                existing.UpdatedAt = Utc(r[5]);
            }
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{rows.Count} done.");
    }

    static async Task MigrateCategories(NpgsqlConnection old, AppDbContext db)
    {
        Console.Write("Categories (top-level)... ");
        var parents = await Query(old,
            "SELECT id, title, position, created_at, updated_at FROM book_categories ORDER BY position");

        foreach (var r in parents)
        {
            var id = (Guid)r[0]!;
            var existing = await db.Categories.FindAsync(id);
            if (existing is null)
                db.Categories.Add(new Category
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Position = (int)r[2]!,
                    ParentId = null,
                    CreatedAt = Utc(r[3]),
                    UpdatedAt = Utc(r[4])
                });
            else
            {
                existing.Title = (string)r[1]!;
                existing.Position = (int)r[2]!;
                existing.UpdatedAt = Utc(r[4]);
            }
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{parents.Count} done.");

        Console.Write("Categories (subcategories)... ");
        var children = await Query(old,
            "SELECT id, title, position, book_category_id, created_at, updated_at FROM book_subcategories ORDER BY position");

        foreach (var r in children)
        {
            var id = (Guid)r[0]!;
            var existing = await db.Categories.FindAsync(id);
            if (existing is null)
                db.Categories.Add(new Category
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Position = (int)r[2]!,
                    ParentId = (Guid)r[3]!,
                    CreatedAt = Utc(r[4]),
                    UpdatedAt = Utc(r[5])
                });
            else
            {
                existing.Title = (string)r[1]!;
                existing.Position = (int)r[2]!;
                existing.ParentId = (Guid)r[3]!;
                existing.UpdatedAt = Utc(r[5]);
            }
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{children.Count} done.");
    }

    static async Task MigrateBooks(NpgsqlConnection old, AppDbContext db)
    {
        Console.Write("Books... ");
        var rows = await Query(old,
            "SELECT id, title, excerpt, publisher, price, language, image_data, document_data, position, published_at, created_at, updated_at, published FROM books ORDER BY position");

        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var coverUrl = ExtractTigrisImageUrl(r[6] as string);
            var documentUrl = ExtractTigrisUrl(r[7] as string);
            var publishedAt = ParsePublishedAt(r[9] as string);

            var existing = await db.Books.FindAsync(id);
            if (existing is null)
                db.Books.Add(new Book
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Excerpt = r[2] as string,
                    Publisher = r[3] as string,
                    Price = r[4] as string,
                    Language = (string)r[5]!,
                    CoverUrl = coverUrl,
                    DocumentUrl = documentUrl,
                    Position = (int)r[8]!,
                    PublishedAt = publishedAt,
                    Published = r[12] is bool b ? b : true,
                    CreatedAt = Utc(r[10]),
                    UpdatedAt = Utc(r[11])
                });
            else
            {
                existing.Title = (string)r[1]!;
                existing.Excerpt = r[2] as string;
                existing.Publisher = r[3] as string;
                existing.Price = r[4] as string;
                existing.Language = (string)r[5]!;
                existing.CoverUrl = coverUrl;
                existing.DocumentUrl = documentUrl;
                existing.Position = (int)r[8]!;
                existing.PublishedAt = publishedAt;
                existing.Published = r[12] is bool b2 ? b2 : true;
                existing.UpdatedAt = Utc(r[11]);
            }
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{rows.Count} done.");
    }

    static async Task MigrateBookAuthors(NpgsqlConnection old, AppDbContext db)
    {
        Console.Write("Book-Author links... ");
        var rows = await Query(old, "SELECT book_id, author_id FROM books_authors");

        foreach (var r in rows)
            await db.Database.ExecuteSqlRawAsync(
                """INSERT INTO book_authors ("BooksId", "AuthorsId") VALUES ({0}, {1}) ON CONFLICT DO NOTHING""",
                r[0]!, r[1]!);

        Console.WriteLine($"{rows.Count} done.");
    }

    static async Task MigrateBookCategories(NpgsqlConnection old, AppDbContext db)
    {
        Console.Write("Book-Category links... ");
        int count = 0;

        var cats = await Query(old, "SELECT book_id, book_category_id FROM book_categorizations");
        foreach (var r in cats)
        {
            await db.Database.ExecuteSqlRawAsync(
                """INSERT INTO book_categories ("BooksId", "CategoriesId") VALUES ({0}, {1}) ON CONFLICT DO NOTHING""",
                r[0]!, r[1]!);
            count++;
        }

        var subcats = await Query(old, "SELECT book_id, book_subcategory_id FROM book_subcategorizations");
        foreach (var r in subcats)
        {
            await db.Database.ExecuteSqlRawAsync(
                """INSERT INTO book_categories ("BooksId", "CategoriesId") VALUES ({0}, {1}) ON CONFLICT DO NOTHING""",
                r[0]!, r[1]!);
            count++;
        }

        Console.WriteLine($"{count} done.");
    }

    static async Task MigrateChapters(NpgsqlConnection old, AppDbContext db)
    {
        Console.Write("Chapters... ");
        var rows = await Query(old,
            "SELECT id, title, body, position, created_at, updated_at, book_id FROM chapters ORDER BY position");

        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var existing = await db.Chapters.FindAsync(id);
            if (existing is null)
                db.Chapters.Add(new Chapter
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Body = r[2] as string,
                    Position = (int)r[3]!,
                    CreatedAt = Utc(r[4]),
                    UpdatedAt = Utc(r[5]),
                    BookId = (Guid)r[6]!
                });
            else
            {
                existing.Title = (string)r[1]!;
                existing.Body = r[2] as string;
                existing.Position = (int)r[3]!;
                existing.UpdatedAt = Utc(r[5]);
            }
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{rows.Count} done.");
    }

    static async Task MigrateSubChapters(NpgsqlConnection old, AppDbContext db)
    {
        Console.Write("SubChapters... ");
        var rows = await Query(old,
            "SELECT id, title, body, position, created_at, updated_at, chapter_id FROM subchapters ORDER BY position");

        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var existing = await db.SubChapters.FindAsync(id);
            if (existing is null)
                db.SubChapters.Add(new SubChapter
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Body = r[2] as string ?? string.Empty,
                    Position = (int)r[3]!,
                    CreatedAt = Utc(r[4]),
                    UpdatedAt = Utc(r[5]),
                    ChapterId = (Guid)r[6]!
                });
            else
            {
                existing.Title = (string)r[1]!;
                existing.Body = r[2] as string ?? string.Empty;
                existing.Position = (int)r[3]!;
                existing.UpdatedAt = Utc(r[5]);
            }
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{rows.Count} done.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static async Task<List<object?[]>> Query(NpgsqlConnection conn, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        var result = new List<object?[]>();
        while (await reader.ReadAsync())
        {
            var row = new object?[reader.FieldCount];
            reader.GetValues(row!);
            // Convert DBNull to null
            for (int i = 0; i < row.Length; i++)
                if (row[i] is DBNull) row[i] = null;
            result.Add(row);
        }
        return result;
    }

    static async Task Exec(NpgsqlConnection conn, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    static DateTime Utc(object? value) =>
        DateTime.SpecifyKind(value is DateTime dt ? dt : DateTime.UtcNow, DateTimeKind.Utc);

    static string? ExtractTigrisUrl(string? jsonb)
    {
        if (string.IsNullOrEmpty(jsonb)) return null;
        try
        {
            var doc = JsonDocument.Parse(jsonb);
            if (doc.RootElement.TryGetProperty("id", out var id))
            {
                var val = id.GetString();
                return string.IsNullOrEmpty(val) ? null : TigrisBase + val;
            }
        }
        catch { }
        return null;
    }

    // image_data is a Shrine derivatives hash: { "original": { "id": "...", ... }, "200": {...}, ... }
    static string? ExtractTigrisImageUrl(string? jsonb)
    {
        if (string.IsNullOrEmpty(jsonb)) return null;
        try
        {
            var root = JsonDocument.Parse(jsonb).RootElement;
            foreach (var variant in new[] { "original", "200", "150", "100", "50" })
            {
                if (root.TryGetProperty(variant, out var v) &&
                    v.TryGetProperty("id", out var id))
                {
                    var val = id.GetString();
                    if (!string.IsNullOrEmpty(val))
                        return TigrisBase + val;
                }
            }
        }
        catch { }
        return null;
    }

    static DateTime? ParsePublishedAt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateTime.TryParse(value, out var dt)
            ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            : null;
    }
}
