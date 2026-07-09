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
        await using var old = await OpenReadOnly(oldConnStr);
        Console.WriteLine("Connected to old DB (read-only). Starting full migration...\n");

        await MigrateAuthors(old, newDb);
        await MigrateCategories(old, newDb);
        await MigrateBooks(old, newDb);
        await MigrateBookAuthors(old, newDb);
        await MigrateBookCategories(old, newDb);
        await MigrateChapters(old, newDb);
        await MigrateSubChapters(old, newDb);

        await CleanupDuplicateAuthors(newDb);

        await MigrateMalfuzat(old, newDb);
        await MigrateMasail(old, newDb);
        await MigrateDua(old, newDb);
        await MigrateBayan(old, newDb);
        await MigrateArticle(old, newDb);

        await MigrateNews(old, newDb);
        await MigrateMadrasah(old, newDb);
        await MigrateNamazTimes(old, newDb);

        Console.WriteLine("\nMigration complete.");
    }

    public static async Task RunNewModulesAsync(string oldConnStr, AppDbContext newDb)
    {
        await using var old = await OpenReadOnly(oldConnStr);
        Console.WriteLine("Connected to old DB (read-only). Migrating News, Madrasah, NamazTimes...\n");

        await MigrateNews(old, newDb);
        await MigrateMadrasah(old, newDb);
        await MigrateNamazTimes(old, newDb);

        Console.WriteLine("\nDone.");
    }

    public static async Task RunPagesAsync(string oldConnStr, AppDbContext newDb)
    {
        await using var old = await OpenReadOnly(oldConnStr);
        Console.WriteLine("Connected to old DB (read-only). Migrating Pages...\n");

        await MigratePages(old, newDb);

        Console.WriteLine("\nDone.");
    }

    static async Task<NpgsqlConnection> OpenReadOnly(string connStr)
    {
        var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();
        await Exec(conn, "SET SESSION CHARACTERISTICS AS TRANSACTION READ ONLY");
        return conn;
    }

    // ── Books (existing) ─────────────────────────────────────────────────────

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

    // ── New-DB author cleanup ─────────────────────────────────────────────────
    // Merge the duplicate "হযরতওয়ালা মুফতী মনসূরুল হক সাহেব" entry that was
    // created from books into the canonical "মুফতী মনসূরুল হক সাহেব" entry,
    // updating all book_authors links so no book loses its author.

    static async Task CleanupDuplicateAuthors(AppDbContext db)
    {
        var canonical = await db.Authors.FirstOrDefaultAsync(a => a.Name == "মুফতী মনসূরুল হক সাহেব");
        var duplicate = await db.Authors.FirstOrDefaultAsync(a => a.Name == "হযরতওয়ালা মুফতী মনসূরুল হক সাহেব");

        if (canonical is null || duplicate is null)
        {
            Console.WriteLine("Author cleanup: entries not found, skipped.");
            return;
        }

        Console.Write("Merging হযরতওয়ালা মুফতী মনসূরুল হক → মুফতী মনসূরুল হক... ");

        // Drop duplicate links where canonical is already present on that book
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM book_authors WHERE \"AuthorsId\" = {0} AND \"BooksId\" IN (SELECT \"BooksId\" FROM book_authors WHERE \"AuthorsId\" = {1})",
            duplicate.Id, canonical.Id);

        // Re-point remaining links to canonical
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE book_authors SET \"AuthorsId\" = {0} WHERE \"AuthorsId\" = {1}",
            canonical.Id, duplicate.Id);

        db.Authors.Remove(duplicate);
        await db.SaveChangesAsync();
        Console.WriteLine("done.");
    }

    // ── Module author migration ───────────────────────────────────────────────
    // Maps per-module author names to the unified Authors table.
    // nameMap: old name → canonical new-system name (missing key = use old name as-is).
    // If the canonical name already exists in the new system → reuse.
    // If not → create new Author using the canonical name.

    static async Task<Dictionary<Guid, Guid>> MigrateModuleAuthors(
        NpgsqlConnection old,
        AppDbContext db,
        string oldTable,
        Dictionary<string, string> nameMap,
        string label)
    {
        Console.Write($"{label} authors... ");

        var existing = await db.Authors.ToListAsync();
        var byName = existing.ToDictionary(a => a.Name.Trim(), a => a.Id);
        int nextPos = existing.Count > 0 ? existing.Max(a => a.Position) + 1 : 1000;

        var rows = await Query(old, $"SELECT id, name, info, position, created_at, updated_at FROM {oldTable} ORDER BY position");
        var map = new Dictionary<Guid, Guid>();

        foreach (var r in rows)
        {
            var oldId = (Guid)r[0]!;
            var oldName = ((string)r[1]!).Trim();
            var canonicalName = nameMap.TryGetValue(oldName, out var mapped) ? mapped : oldName;

            if (byName.TryGetValue(canonicalName, out var newId))
            {
                map[oldId] = newId;
            }
            else
            {
                var author = new Author
                {
                    Id = Guid.NewGuid(),
                    Name = canonicalName,
                    Info = r[2] as string,
                    Position = nextPos++,
                    CreatedAt = Utc(r[4]),
                    UpdatedAt = Utc(r[5])
                };
                db.Authors.Add(author);
                await db.SaveChangesAsync();
                byName[canonicalName] = author.Id;
                map[oldId] = author.Id;
            }
        }

        Console.WriteLine($"{rows.Count} done.");
        return map;
    }

    // ── Author name maps ──────────────────────────────────────────────────────
    // key = exact name from old module table, value = canonical name in new system.
    // Only near-matches need entries; exact matches and genuinely new authors
    // are handled automatically by the name-lookup logic.

    static readonly Dictionary<string, string> MalfuzatAuthorMap = new()
    {
        ["হযরত সাইয়্যিদ মাওলানা আবরারুল হক সাহেব রহ."] = "শাহ সাইয়্যিদ আবরারুল হক সাহেব",
        // invisible zero-width joiner in new system name:
        ["মুফতী মীযানুর রহমান কাসেমী সাহেব (রাহমানিয়া)"] = "মুফতী ‍মীযানুর রহমান কাসেমী সাহেব (রাহমানিয়া)",
    };

    static readonly Dictionary<string, string> MasailAuthorMap = new()
    {
        // masail মীযানুর entry already has the zero-width joiner, matches new system exactly
    };

    static readonly Dictionary<string, string> BayanAuthorMap = new()
    {
        ["হযরতওয়ালা মুফতী মনসূরুল হক সাহেব"] = "মুফতী মনসূরুল হক সাহেব",
        ["মাওলানা আবুল হাসান আলী নদভী রহ."] = "মাওলানা সৈয়দ আবুল হাসান আলী নদভী রহ.",
        ["মাওলানা ইউসুফ কান্ধলভী রহ. (২য় হযরতজী)"] = "হযরতজী মাওলানা ইউসুফ কান্ধলভী রহ.",
        ["মাওলানা ইদরীস কান্ধলভী রহ."] = "আল্লামা ইদ্রীস কান্ধলভী রহ.",
        ["মাওলানা কালিম সিদ্দিকী সাহেব"] = "মাওলাা কালীম সিদ্দীকি সাহেব",
        ["হযরত মাওলানা হাকীম মুহাম্মাদ আখতার সাহেব রহ."] = "মাওলানা শাহ হাকীম মুহাম্মাদ আখতার রহ.",
        ["হযরত যুলফিকার আহমাদ নকশবন্দী"] = "মাওলানা যুলফিকার নকশবন্দী",
        ["হযরত সাইয়্যিদ মাওলানা শাহ আবরারুল হক সাহেব রহ."] = "শাহ সাইয়্যিদ আবরারুল হক সাহেব",
        ["মাওলানা সাখওয়াত সাহেব (মুবাল্লিগ)"] = "মাওলানা সাখাওয়াতুল্লাহ সাহেব",
    };

    static readonly Dictionary<string, string> ArticleAuthorMap = new()
    {
        ["Mufti Mansurul Haq Saheb"] = "মুফতী মনসূরুল হক সাহেব",
        ["হযরতওয়ালা মুফতী মনসূরুল হক সাহেব"] = "মুফতী মনসূরুল হক সাহেব",
    };

    // ── Module category migration ─────────────────────────────────────────────
    // Maps old module category IDs to new unified Categories table.
    // Uses a title-based mapping dict; null value = create new with old title.

    static async Task<Dictionary<Guid, Guid>> BuildModuleCategoryMap(
        NpgsqlConnection old,
        AppDbContext db,
        string oldTable,
        Dictionary<string, string?> titleMap,
        string label)
    {
        Console.Write($"{label} categories... ");

        // Load current categories by title (updated per call so cross-module new cats are visible)
        var allCats = await db.Categories.ToListAsync();
        var byTitle = allCats.ToDictionary(c => c.Title, c => c.Id);
        int nextPos = allCats.Count > 0 ? allCats.Max(c => c.Position) + 1 : 100;

        var rows = await Query(old, $"SELECT id, title FROM {oldTable} ORDER BY position");
        var map = new Dictionary<Guid, Guid>();

        foreach (var r in rows)
        {
            var oldId = (Guid)r[0]!;
            var oldTitle = ((string)r[1]!).Trim();

            // Determine the target new-system category title
            string newTitle = titleMap.TryGetValue(oldTitle, out var mapped) && mapped != null
                ? mapped
                : oldTitle; // null mapping or missing = keep old title as new category

            if (byTitle.TryGetValue(newTitle, out var newId))
            {
                map[oldId] = newId;
            }
            else
            {
                var cat = new Category
                {
                    Id = Guid.NewGuid(),
                    Title = newTitle,
                    Position = nextPos++,
                    ParentId = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Categories.Add(cat);
                await db.SaveChangesAsync();
                byTitle[newTitle] = cat.Id;
                map[oldId] = cat.Id;
            }
        }

        Console.WriteLine($"{rows.Count} mapped.");
        return map;
    }

    // ── Category title maps ───────────────────────────────────────────────────
    // key = old category title, value = new system category title (null = create new with old title)

    static readonly Dictionary<string, string?> MalfuzatCatMap = new()
    {
        ["ঈমান ও আক্বাইদ"] = "ঈমান আক্বাইদ",
        ["ইসলাহে নফস"] = "ইসলাহে নফস",
        ["কুরআন ও তাফসীর"] = "কুরআন ও তাফসীর",
        ["দাওয়াত ও তাবলীগ"] = "দাওয়াত ও তাবলীগ",
        ["নামায"] = "নামায",
        ["বান্দার হক"] = "বান্দার হক",
        ["বিবিধ"] = "বিবিধ",
        ["গুনাহ শিরক কুফর ও বিদ'আত"] = "গুনাহ ও বিদ'আত",
        ["ভ্রান্ত ও গোমরাহ দল"] = "ভ্রান্ত দল",
        ["বিবাহ ও তালাক"] = "বিবাহ তালাক",
        ["দু'আ দুরূদ ও যিকির"] = "দু'আ দুরূদ",
        ["মেয়েদের প্রসঙ্গ"] = "মেয়েদের বিষয়",
        ["ব্যবসা ও লেনদেন"] = "লেনদেন কামাই রোজগার",
        ["তোলাবা ও উলামাদের দায়িত্ব"] = "উলামা তোলাবা",
        ["যাকাত ফিতরা ও দান"] = "রোযা যাকাত ও ইতিকাফ",
        ["রোযা"] = "রোযা যাকাত ও ইতিকাফ",
        ["হাজ্জ উমরাহ ও কুরবানী"] = "হাজ্জ",
        ["সুন্নাহ ও হাদীস"] = "সুন্নত",
        ["আল্লাহওয়ালা"] = "আল্লাহওয়ালাগণের যিন্দেগী",
        ["আদব আখলাক"] = "ইসলাহে নফস",
        ["বর্তমান প্রেক্ষাপট ও ইতিহাস"] = "অন্যান্য ঘটনা ইতিহাস",
        ["দীন শিক্ষা ও তাহ্ক্বীক্বাত"] = null,
        ["রাজনীতি ও ব্রিটিশের আগ্রাশন"] = null,
        ["স্বাস্থ্যবিধি"] = null,
    };

    static readonly Dictionary<string, string?> MasailCatMap = new()
    {
        ["ঈমান ও আক্বাইদ"] = "ঈমান আক্বাইদ",
        ["দাওয়াত ও তাবলীগ"] = "দাওয়াত ও তাবলীগ",
        ["নামায"] = "নামায",
        ["বান্দার হক"] = "বান্দার হক",
        ["বিবাহ তালাক"] = "বিবাহ তালাক",
        ["বিবিধ"] = "বিবিধ",
        ["মসজিদ মাদরাসা"] = "মসজিদ মাদরাসা",
        ["কাফন দাফন জানাযা কবর যিয়ারত"] = "কাফন-দাফন",
        ["কুরআন তাফসীর ও হাদীস"] = "কুরআন ও তাফসীর",
        ["কুরবানী ও আক্বিকা শিকার ও জবেহ ও হালাল খাদ্য"] = "কুরবানী ও আক্বিকা",
        ["দু'আ দুরূদ ও যিকির"] = "দু'আ দুরূদ",
        ["বাতিল সম্প্রদায় ও গোমরাহ দল"] = "ভ্রান্ত দল",
        ["মেয়েদের প্রসঙ্গ"] = "মেয়েদের বিষয়",
        ["রোযা ইতিকাফ ও ঈদ"] = "রোযা যাকাত ও ইতিকাফ",
        ["লেনদেন ও ব্যবসা বানিজ্য"] = "লেনদেন কামাই রোজগার",
        ["হাজ্জ ও উমরাহ"] = "হাজ্জ",
        ["সিরাত নবী সাহাবীদের ঘটনা ও ইতিহাস"] = "সিরাত ও নবী আ. এর যিন্দেগী",
        ["ইলমে দীন ও উলামা তোলাবা"] = "উলামা তোলাবা",
        ["কুফর শিরক বিদ'আত কুসংস্কার অপসংস্কৃতি"] = "গুনাহ ও বিদ'আত",
        ["যাকাত ফিতরা দান উশর ট্যাক্স"] = "রোযা যাকাত ও ইতিকাফ",
        ["সুন্নত ও হাদীস"] = "সুন্নত",
        ["তাসাউফ ও আত্মশুদ্ধি"] = "ইসলাহে নফস",
        ["ব্যাংক বীমা ইন্সুরেন্স"] = "লেনদেন কামাই রোজগার",
        ["ফাতাওয়া প্রসঙ্গ ও এর গুরুত্ব"] = "মাসআলা-মাসাইল",
        ["আযান ইক্বামাত ও ইমাম মু'আযযিন"] = "নামায",
        ["পর্দা"] = "মেয়েদের বিষয়",
        ["অসুস্থ ও মাযূর"] = null,
        ["আইন ও দন্ডবিধি"] = null,
        ["উযূ গোসল ইস্তেন্জা তায়াম্মুম ও মাসাহ"] = null,
        ["কসম ও মান্নত"] = null,
        ["কাপড় পানি যায়গা পাক নাপাক"] = null,
        ["কাযা কাফফারা ও ফিদয়া"] = null,
        ["জিহাদ"] = null,
        ["তাবীজ-কবচ তাদবীর"] = null,
        ["পোশাক পরিচ্ছদ"] = null,
        ["মাযহাব ও তাকলীদ"] = null,
        ["মিরাস বন্টন"] = null,
        ["সফর ও মুসাফির"] = null,
        ["সামাজিকতা আচার ব্যবহার ও রাজনীতি"] = null,
        ["হায়েয নেফায"] = null,
    };

    static readonly Dictionary<string, string?> DuaCatMap = new()
    {
        ["দুরূদ শরীফ প্রসঙ্গ"] = "দু'আ দুরূদ",
        ["নামাযের ভিতরের দু'আ ও তাসবীহ"] = "দু'আ দুরূদ",
        ["পবিত্র কুরআন থেকে সংগৃহীত দু'আ প্রসঙ্গ"] = "দু'আ দুরূদ",
        ["ফরজ নামাযের পর দু'আ ও আমল"] = "দু'আ দুরূদ",
        ["বিভিন্ন স্থান ও সময়ের দু'আ"] = "দু'আ দুরূদ",
        ["মুনাজাত"] = "দু'আ দুরূদ",
        ["মুনাজাতে মাকবূল"] = "দু'আ দুরূদ",
        ["সকাল-সন্ধ্যার আমল ও দু'আ প্রসঙ্গ"] = "দু'আ দুরূদ",
        ["হাদীস শরীফ থেকে সংগৃহীত দু'আ"] = "দু'আ দুরূদ",
        ["উলামায়েকেরাম থেকে সংগৃহিত দু'আ"] = "দু'আ দুরূদ",
        ["ইস্তিগফার প্রসঙ্গ"] = "দু'আ দুরূদ",
        ["অত্যন্ত ফযীলতপূর্ণ বিশেষ কিছু আমল"] = "বারো মাসের আমল",
        ["ফযিলতপূর্ণ সূরা ও আয়াত"] = "কুরআন ও তাফসীর",
        ["কিছু নফল নামাযের প্রসঙ্গ"] = "নামায",
        ["ভূমিকা"] = "বিবিধ",
        ["মানযিল (জ্বীন-শয়তান থেকে বাঁচার আমাল)"] = null,
        ["রুকইয়াহ ও তাদবীর বা জ্বীনের চিকিৎসা"] = null,
    };

    static readonly Dictionary<string, string?> BayanCatMap = new()
    {
        ["গুনাহ ও বিদ'আত"] = "গুনাহ ও বিদ'আত",
        ["কুরবানী ও আক্বিকা"] = "কুরবানী ও আক্বিকা",
        ["উলামা তোলাবা"] = "উলামা তোলাবা",
        ["দু'আ দুরূদ"] = "দু'আ দুরূদ",
        ["নামায"] = "নামায",
        ["বান্দার হক"] = "বান্দার হক",
        ["বারো মাসের আমল"] = "বারো মাসের আমল",
        ["ভ্রান্ত দল"] = "ভ্রান্ত দল",
        ["সুন্নত"] = "সুন্নত",
        ["হাজ্জ"] = "হাজ্জ",
        ["ঈমান-আাক্বিদা"] = "ঈমান আক্বাইদ",
        ["কুরআন ও তাফসীরে কুরআন"] = "কুরআন ও তাফসীর",
        ["তাবলীগ"] = "দাওয়াত ও তাবলীগ",
        ["দারসে হাদীস"] = "হাদীস ও শরাহ",
        ["নবী-রসূল, সাহাবী ও আকাবীরদের জীবনী"] = "সিরাত ও নবী আ. এর যিন্দেগী",
        ["বিবাহ"] = "বিবাহ তালাক",
        ["মহিলাদের বিষয়"] = "মেয়েদের বিষয়",
        ["মাসাইল"] = "মাসআলা-মাসাইল",
        ["রোযা, যাকাত ও ইতিকাফ"] = "রোযা যাকাত ও ইতিকাফ",
        ["লেনদেন ও কামাই রোজগার"] = "লেনদেন কামাই রোজগার",
        ["তাযকিয়া"] = "ইসলাহে নফস",
        ["লা মাযহাবীদের ভ্রান্তি"] = "ভ্রান্ত দল",
        ["Urdu Bayan"] = null,
        ["ইজতিমা"] = null,
        ["ইবাদাত"] = null,
        ["ঈদ প্রসঙ্গ"] = null,
        ["খানকায়ে আবরারিয়া"] = null,
        ["জুম'আ"] = null,
        ["তারবিয়াতী জলসা"] = null,
        ["তালীম"] = null,
        ["দাওয়াতুল হক"] = null,
        ["মাহফিল"] = null,
    };

    static readonly Dictionary<string, string?> ArticleCatMap = new()
    {
        ["দাওয়াত ও তাবলীগ"] = "দাওয়াত ও তাবলীগ",
        ["নামায"] = "নামায",
        ["বান্দার হক"] = "বান্দার হক",
        ["বিবাহ তালাক"] = "বিবাহ তালাক",
        ["বিবিধ"] = "বিবিধ",
        ["ঈমান ও আক্বিদা"] = "ঈমান আক্বাইদ",
        ["দু'আ দুরূদ ও যিকির"] = "দু'আ দুরূদ",
        ["মাদরাসা মাসজিদ ও দীন শিক্ষা"] = "মসজিদ মাদরাসা",
        ["মেয়েদের প্রসঙ্গ"] = "মেয়েদের বিষয়",
        ["বারো মাসের করণীয় ও বর্জনীয়"] = "বারো মাসের আমল",
        ["লেনদেন ও বেচা কেনা"] = "লেনদেন কামাই রোজগার",
        ["আত্মশুদ্ধি ও আদব আখলাক"] = "ইসলাহে নফস",
        ["ইতিহাস ও জীবনী"] = "অন্যান্য ঘটনা ইতিহাস",
        ["অপসংস্কৃতি ও বাতিল সম্প্রদায়"] = "ভ্রান্ত দল",
        ["যাকাত ফিতরা ও দান"] = "রোযা যাকাত ও ইতিকাফ",
        ["রোযা"] = "রোযা যাকাত ও ইতিকাফ",
        ["হাজ্জ উমরাহ ও কুরবানী"] = "হাজ্জ",
        ["বর্তমান প্রেক্ষাপট রাজনীতি ও সামাজিকতা"] = null,
        ["হালাল-হারাম ও জায়োয নাজায়েয"] = null,
    };

    // ── Malfuzat ─────────────────────────────────────────────────────────────

    static async Task MigrateMalfuzat(NpgsqlConnection old, AppDbContext db)
    {
        Console.WriteLine("\n-- Malfuzat --");

        var authorMap = await MigrateModuleAuthors(old, db, "malfuzat_authors", MalfuzatAuthorMap, "Malfuzat");
        var catMap = await BuildModuleCategoryMap(old, db, "malfuzat_categories", MalfuzatCatMap, "Malfuzat");

        Console.Write("Malfuzat records... ");
        var rows = await Query(old,
            "SELECT id, title, body, excerpt, language, audio_data, document_data, has_audio, published, published_at, position, created_at, updated_at, malfuzat_author_id FROM malfuzats ORDER BY position NULLS LAST");

        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var oldAuthorId = (Guid)r[13]!;
            if (!authorMap.TryGetValue(oldAuthorId, out var newAuthorId)) continue;

            var existing = await db.Malfuzats.FindAsync(id);
            if (existing is null)
                db.Malfuzats.Add(new Malfuzat
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Body = r[2] as string,
                    Excerpt = r[3] as string,
                    Language = (string)r[4]!,
                    AudioUrl = ExtractTigrisUrl(r[5] as string),
                    DocumentUrl = ExtractTigrisUrl(r[6] as string),
                    HasAudio = r[7] is bool ha ? ha : false,
                    Published = r[8] is bool pub ? pub : true,
                    PublishedAt = r[9] is DateTime pat ? DateTime.SpecifyKind(pat, DateTimeKind.Utc) : null,
                    Position = r[10] as int?,
                    CreatedAt = Utc(r[11]),
                    UpdatedAt = Utc(r[12]),
                    AuthorId = newAuthorId
                });
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{rows.Count} done.");

        Console.Write("Malfuzat-Category links... ");
        var links = await Query(old, "SELECT malfuzat_id, malfuzat_category_id FROM malfuzat_categorizations");
        int linkCount = 0;
        foreach (var r in links)
        {
            var itemId = (Guid)r[0]!;
            var oldCatId = (Guid)r[1]!;
            if (!catMap.TryGetValue(oldCatId, out var newCatId)) continue;
            await db.Database.ExecuteSqlRawAsync(
                """INSERT INTO malfuzat_categories ("MalfuzatsId", "CategoriesId") VALUES ({0}, {1}) ON CONFLICT DO NOTHING""",
                itemId, newCatId);
            linkCount++;
        }
        Console.WriteLine($"{linkCount} done.");
    }

    // ── Masail ────────────────────────────────────────────────────────────────

    static async Task MigrateMasail(NpgsqlConnection old, AppDbContext db)
    {
        Console.WriteLine("\n-- Masail --");

        var authorMap = await MigrateModuleAuthors(old, db, "masail_authors", MasailAuthorMap, "Masail");
        var catMap = await BuildModuleCategoryMap(old, db, "masail_categories", MasailCatMap, "Masail");

        Console.Write("Masail records... ");
        var rows = await Query(old,
            "SELECT id, title, question, answer, language, audio_data, document_data, has_audio, published, published_at, position, created_at, updated_at, masail_author_id FROM masails ORDER BY position");

        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var oldAuthorId = r[13] as Guid?;
            Guid? newAuthorId = oldAuthorId.HasValue && authorMap.TryGetValue(oldAuthorId.Value, out var aid)
                ? aid : null;

            var existing = await db.Masails.FindAsync(id);
            if (existing is null)
                db.Masails.Add(new Masail
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Question = (string)r[2]!,
                    Answer = r[3] as string,
                    Language = (string)r[4]!,
                    AudioUrl = ExtractTigrisUrl(r[5] as string),
                    DocumentUrl = ExtractTigrisUrl(r[6] as string),
                    HasAudio = r[7] is bool ha ? ha : false,
                    Published = r[8] is bool pub ? pub : true,
                    PublishedAt = r[9] is DateTime pat ? DateTime.SpecifyKind(pat, DateTimeKind.Utc) : null,
                    Position = (int)r[10]!,
                    CreatedAt = Utc(r[11]),
                    UpdatedAt = Utc(r[12]),
                    AuthorId = newAuthorId
                });
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{rows.Count} done.");

        Console.Write("Masail-Category links... ");
        var links = await Query(old, "SELECT masail_id, masail_category_id FROM masail_categorizations");
        int linkCount = 0;
        foreach (var r in links)
        {
            var itemId = (Guid)r[0]!;
            var oldCatId = (Guid)r[1]!;
            if (!catMap.TryGetValue(oldCatId, out var newCatId)) continue;
            await db.Database.ExecuteSqlRawAsync(
                """INSERT INTO masail_categories ("MasailsId", "CategoriesId") VALUES ({0}, {1}) ON CONFLICT DO NOTHING""",
                itemId, newCatId);
            linkCount++;
        }
        Console.WriteLine($"{linkCount} done.");
    }

    // ── Dua ───────────────────────────────────────────────────────────────────

    static async Task MigrateDua(NpgsqlConnection old, AppDbContext db)
    {
        Console.WriteLine("\n-- Dua --");

        var catMap = await BuildModuleCategoryMap(old, db, "dua_categories", DuaCatMap, "Dua");

        Console.Write("Dua records... ");
        var rows = await Query(old,
            "SELECT id, title, body, excerpt, language, audio_data, document_data, published, position, created_at, updated_at FROM duas ORDER BY position");

        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var existing = await db.Duas.FindAsync(id);
            if (existing is null)
                db.Duas.Add(new Dua
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Body = (string)r[2]!,
                    Excerpt = r[3] as string,
                    Language = (string)r[4]!,
                    AudioUrl = ExtractTigrisUrl(r[5] as string),
                    DocumentUrl = ExtractTigrisUrl(r[6] as string),
                    Published = r[7] is bool pub ? pub : true,
                    Position = (int)r[8]!,
                    CreatedAt = Utc(r[9]),
                    UpdatedAt = Utc(r[10])
                });
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{rows.Count} done.");

        Console.Write("Dua-Category links... ");
        var links = await Query(old, "SELECT dua_id, dua_category_id FROM dua_categorizations");
        int linkCount = 0;
        foreach (var r in links)
        {
            var itemId = (Guid)r[0]!;
            var oldCatId = (Guid)r[1]!;
            if (!catMap.TryGetValue(oldCatId, out var newCatId)) continue;
            await db.Database.ExecuteSqlRawAsync(
                """INSERT INTO dua_categories ("DuasId", "CategoriesId") VALUES ({0}, {1}) ON CONFLICT DO NOTHING""",
                itemId, newCatId);
            linkCount++;
        }
        Console.WriteLine($"{linkCount} done.");
    }

    // ── Bayan ─────────────────────────────────────────────────────────────────

    static async Task MigrateBayan(NpgsqlConnection old, AppDbContext db)
    {
        Console.WriteLine("\n-- Bayan --");

        // Speakers → Authors
        var authorMap = await MigrateModuleAuthors(old, db, "speakers", BayanAuthorMap, "Bayan (speakers)");

        var catMap = await BuildModuleCategoryMap(old, db, "bayan_categories", BayanCatMap, "Bayan");

        Console.Write("Bayan records... ");
        var rows = await Query(old,
            "SELECT id, title, excerpt, language, location, audio_data, published, published_at, position, created_at, updated_at, speaker_id FROM bayans ORDER BY position");

        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var oldSpeakerId = (Guid)r[11]!;
            if (!authorMap.TryGetValue(oldSpeakerId, out var newAuthorId)) continue;

            var existing = await db.Bayans.FindAsync(id);
            if (existing is null)
                db.Bayans.Add(new Bayan
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Excerpt = r[2] as string,
                    Language = (string)r[3]!,
                    Location = r[4] as string,
                    AudioUrl = ExtractTigrisUrl(r[5] as string),
                    Published = r[6] is bool pub ? pub : true,
                    PublishedAt = r[7] is DateTime pat
                        ? DateTime.SpecifyKind(pat, DateTimeKind.Utc)
                        : DateTime.UtcNow,
                    Position = (int)r[8]!,
                    CreatedAt = Utc(r[9]),
                    UpdatedAt = Utc(r[10]),
                    AuthorId = newAuthorId
                });
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{rows.Count} done.");

        Console.Write("Bayan-Category links... ");
        var links = await Query(old, "SELECT bayan_id, bayan_category_id FROM bayan_categorizations");
        int linkCount = 0;
        foreach (var r in links)
        {
            var itemId = (Guid)r[0]!;
            var oldCatId = (Guid)r[1]!;
            if (!catMap.TryGetValue(oldCatId, out var newCatId)) continue;
            await db.Database.ExecuteSqlRawAsync(
                """INSERT INTO bayan_categories ("BayansId", "CategoriesId") VALUES ({0}, {1}) ON CONFLICT DO NOTHING""",
                itemId, newCatId);
            linkCount++;
        }
        Console.WriteLine($"{linkCount} done.");
    }

    // ── Article ───────────────────────────────────────────────────────────────

    static async Task MigrateArticle(NpgsqlConnection old, AppDbContext db)
    {
        Console.WriteLine("\n-- Article --");

        var authorMap = await MigrateModuleAuthors(old, db, "article_authors", ArticleAuthorMap, "Article");

        var catMap = await BuildModuleCategoryMap(old, db, "article_categories", ArticleCatMap, "Article");

        Console.Write("Article records... ");
        var rows = await Query(old,
            "SELECT id, title, excerpt, body, language, document_data, published, published_at, position, created_at, updated_at, article_author_id FROM articles ORDER BY position NULLS LAST");

        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var oldAuthorId = r[11] as Guid?;
            Guid? newAuthorId = oldAuthorId.HasValue && authorMap.TryGetValue(oldAuthorId.Value, out var aid)
                ? aid : null;

            var existing = await db.Articles.FindAsync(id);
            if (existing is null)
                db.Articles.Add(new Article
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Excerpt = r[2] as string,
                    Body = (string)r[3]!,
                    Language = (string)r[4]!,
                    DocumentUrl = ExtractTigrisUrl(r[5] as string),
                    Published = r[6] is bool pub ? pub : true,
                    PublishedAt = r[7] is DateTime pat ? DateTime.SpecifyKind(pat, DateTimeKind.Utc) : null,
                    Position = r[8] as int?,
                    CreatedAt = Utc(r[9]),
                    UpdatedAt = Utc(r[10]),
                    AuthorId = newAuthorId
                });
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{rows.Count} done.");

        Console.Write("Article-Category links... ");
        var links = await Query(old, "SELECT article_id, article_category_id FROM article_categorizations");
        int linkCount = 0;
        foreach (var r in links)
        {
            var itemId = (Guid)r[0]!;
            var oldCatId = (Guid)r[1]!;
            if (!catMap.TryGetValue(oldCatId, out var newCatId)) continue;
            await db.Database.ExecuteSqlRawAsync(
                """INSERT INTO article_categories ("ArticlesId", "CategoriesId") VALUES ({0}, {1}) ON CONFLICT DO NOTHING""",
                itemId, newCatId);
            linkCount++;
        }
        Console.WriteLine($"{linkCount} done.");
    }

    // ── News ──────────────────────────────────────────────────────────────────

    static async Task MigrateNews(NpgsqlConnection old, AppDbContext db)
    {
        Console.WriteLine("\n-- News --");
        Console.Write("News records... ");

        // Old table has no position column — order by published_at and assign sequentially
        var rows = await Query(old,
            "SELECT id, title, excerpt, body, language, published, published_at, created_at, updated_at FROM news ORDER BY published_at ASC");

        int position = 1;
        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var existing = await db.News.FindAsync(id);
            if (existing is null)
                db.News.Add(new News
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Excerpt = r[2] as string,
                    Body = (string)r[3]!,
                    Language = (string)r[4]!,
                    Published = r[5] is bool pub ? pub : true,
                    PublishedAt = r[6] is DateTime pat ? DateTime.SpecifyKind(pat, DateTimeKind.Utc) : null,
                    Position = position,
                    CreatedAt = Utc(r[7]),
                    UpdatedAt = Utc(r[8])
                });
            else
            {
                existing.Title = (string)r[1]!;
                existing.Excerpt = r[2] as string;
                existing.Body = (string)r[3]!;
                existing.Language = (string)r[4]!;
                existing.Published = r[5] is bool pub2 ? pub2 : true;
                existing.PublishedAt = r[6] is DateTime pat2 ? DateTime.SpecifyKind(pat2, DateTimeKind.Utc) : null;
                existing.UpdatedAt = Utc(r[8]);
            }
            position++;
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{rows.Count} done.");
    }

    // ── Madrasah ──────────────────────────────────────────────────────────────

    static async Task MigrateMadrasah(NpgsqlConnection old, AppDbContext db)
    {
        Console.WriteLine("\n-- Madrasah --");
        Console.Write("Madrasah records... ");

        var rows = await Query(old,
            "SELECT id, title, excerpt, introduction, position, created_at, updated_at FROM madrasahs ORDER BY position");

        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var existing = await db.Madrasahs.FindAsync(id);
            if (existing is null)
                db.Madrasahs.Add(new Madrasah
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Excerpt = r[2] as string,
                    Introduction = (string)r[3]!,
                    Position = (int)r[4]!,
                    CreatedAt = Utc(r[5]),
                    UpdatedAt = Utc(r[6])
                });
            else
            {
                existing.Title = (string)r[1]!;
                existing.Excerpt = r[2] as string;
                existing.Introduction = (string)r[3]!;
                existing.Position = (int)r[4]!;
                existing.UpdatedAt = Utc(r[6]);
            }
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{rows.Count} done.");

        Console.Write("Madrasah info fields... ");
        var infos = await Query(old,
            "SELECT id, label, info, position, created_at, updated_at, madrasah_id FROM madrasah_infos ORDER BY madrasah_id, position");

        foreach (var r in infos)
        {
            var id = (Guid)r[0]!;
            var existing = await db.MadrasahInfos.FindAsync(id);
            if (existing is null)
                db.MadrasahInfos.Add(new MadrasahInfo
                {
                    Id = id,
                    Label = (string)r[1]!,
                    Info = (string)r[2]!,
                    Position = (int)r[3]!,
                    CreatedAt = Utc(r[4]),
                    UpdatedAt = Utc(r[5]),
                    MadrasahId = (Guid)r[6]!
                });
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{infos.Count} done.");

        Console.Write("Madrasah photos... ");
        var photos = await Query(old,
            "SELECT id, title, image_data, position, created_at, updated_at, madrasah_id FROM madrasah_photos ORDER BY madrasah_id, position");

        foreach (var r in photos)
        {
            var id = (Guid)r[0]!;
            var imageUrl = ExtractTigrisImageUrl(r[2] as string);
            if (string.IsNullOrEmpty(imageUrl)) continue;

            var existing = await db.MadrasahPhotos.FindAsync(id);
            if (existing is null)
                db.MadrasahPhotos.Add(new MadrasahPhoto
                {
                    Id = id,
                    Title = (string)r[1]!,
                    ImageUrl = imageUrl,
                    Position = (int)r[3]!,
                    CreatedAt = Utc(r[4]),
                    UpdatedAt = Utc(r[5]),
                    MadrasahId = (Guid)r[6]!
                });
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{photos.Count} done.");
    }

    // ── NamazTimes ────────────────────────────────────────────────────────────

    static async Task MigrateNamazTimes(NpgsqlConnection old, AppDbContext db)
    {
        Console.WriteLine("\n-- NamazTimes --");
        Console.Write("NamazTime records... ");

        // position was added later and may be null — fall back to row order
        var rows = await Query(old,
            "SELECT id, title, title_bn, masail, fazail, position, created_at, updated_at FROM namaz_times ORDER BY COALESCE(position, 999), created_at");

        int fallbackPos = 1;
        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var position = r[5] as int? ?? fallbackPos;

            var existing = await db.NamazTimes.FindAsync(id);
            if (existing is null)
                db.NamazTimes.Add(new NamazTime
                {
                    Id = id,
                    Title = (string)r[1]!,
                    TitleBn = r[2] as string,
                    Masail = (string)r[3]!,
                    Fazail = r[4] as string,
                    Position = position,
                    CreatedAt = Utc(r[6]),
                    UpdatedAt = Utc(r[7])
                });
            else
            {
                existing.Title = (string)r[1]!;
                existing.TitleBn = r[2] as string;
                existing.Masail = (string)r[3]!;
                existing.Fazail = r[4] as string;
                existing.Position = position;
                existing.UpdatedAt = Utc(r[7]);
            }
            fallbackPos++;
        }
        await db.SaveChangesAsync();
        Console.WriteLine($"{rows.Count} done.");
    }

    // ── Pages ─────────────────────────────────────────────────────────────────

    static async Task MigratePages(NpgsqlConnection old, AppDbContext db)
    {
        Console.WriteLine("\n-- Pages --");
        Console.Write("Page records... ");

        var rows = await Query(old,
            "SELECT id, title, slug, body, image_data, created_at, updated_at FROM pages ORDER BY created_at");

        foreach (var r in rows)
        {
            var id = (Guid)r[0]!;
            var imageUrl = ExtractTigrisImageUrl(r[4] as string);

            var existing = await db.Pages.FindAsync(id);
            if (existing is null)
                db.Pages.Add(new Page
                {
                    Id = id,
                    Title = (string)r[1]!,
                    Slug = (string)r[2]!,
                    Body = (string)r[3]!,
                    ImageUrl = imageUrl,
                    CreatedAt = Utc(r[5]),
                    UpdatedAt = Utc(r[6])
                });
            else
            {
                existing.Title = (string)r[1]!;
                existing.Slug = (string)r[2]!;
                existing.Body = (string)r[3]!;
                existing.ImageUrl = imageUrl;
                existing.UpdatedAt = Utc(r[6]);
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
