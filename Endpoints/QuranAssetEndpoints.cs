using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace IslamiJindegiApi.Endpoints;

public static class QuranAssetEndpoints
{
    const string CdnBase = "https://static.islamijindegi.com";

    record MushafEditionConfig(
        string Id,
        string Title,
        int    Width,
        int    Height,
        string Ext,
        int    TotalPages,
        string PagesBaseUrl,
        string AyahBoxesUrl
    );

    static MushafEditionConfig BuildConfig(string id, string title, int w, int h, string ext, int totalPages) => new(
        Id:          id,
        Title:       title,
        Width:       w,
        Height:      h,
        Ext:         ext,
        TotalPages:  totalPages,
        PagesBaseUrl: $"{CdnBase}/mushaf/{id}/pages",
        AyahBoxesUrl: $"{CdnBase}/mushaf/{id}/ayah_boxes.json"
    );

    static readonly MushafEditionConfig[] Editions =
    [
        BuildConfig("imdadia_hafezi",   "হাফিজি কুরআন (এমদাদিয়া লাইব্রেরী)", 1152, 2048, "jpg", 612),
        BuildConfig("hafezi",           "হাফিজি কুরআন",                          1152, 2048, "png", 612),
        BuildConfig("colorful_tajweed", "রঙিন তাজবীদ কুরআন",                     720,  1057, "png", 850),
        BuildConfig("madani",           "মাদানী কুরআন (উসমানী প্রিন্ট)",          1352, 2170, "png", 606),
        BuildConfig("nurani",           "নূরানী কুরআন (এমদাদিয়া লাইব্রেরী)",    670,  996,  "png", 730),
        BuildConfig("colorful_hafezi",  "রঙিন হাফিজি",                            560,  829,  "jpg", 612),
    ];

    static readonly HashSet<string> ValidMushafs =
    [
        "imdadia_hafezi", "hafezi", "colorful_tajweed",
        "madani", "nurani", "colorful_hafezi",
    ];

    static readonly HashSet<string> ValidTafsirs =
    [
        "tafsir_taqi_usmani_bn", "tafsir_ibn_kathir_bn",
        "tafsir_ibn_kathir_en", "tafsir_maariful_quran_bn",
        "tafsir_maariful_quran_en",
    ];

    static readonly HashSet<string> ValidDbs =
    [
        "articles", "bayans", "books", "duas",
        "madrasahs", "malfuzats", "masails", "misc",
    ];

    // Per-sura ayah counts (index 0 = Sura 1 Al-Fatiha)
    static readonly int[] AyahCounts =
    [
          7, 286, 200, 176, 120, 165, 206,  75, 129, 109,
        123, 111,  43,  52,  99, 128, 111, 110,  98, 135,
        112,  78, 118,  64,  77, 227,  93,  88,  69,  60,
         34,  30,  73,  54,  45,  83, 182,  88,  75,  85,
         54,  53,  89,  59,  37,  35,  38,  29,  18,  45,
         60,  49,  62,  55,  78,  96,  29,  22,  24,  13,
         14,  11,  11,  18,  12,  12,  30,  52,  52,  44,
         28,  28,  20,  56,  40,  31,  50,  40,  46,  42,
         29,  19,  36,  25,  22,  17,  19,  26,  30,  20,
         15,  21,  11,   8,   8,  19,   5,   8,   8,  11,
         11,   8,   3,   9,   5,   4,   7,   3,   6,   3,
          5,   4,   5,   6,
    ];

    public static void MapQuranAssetEndpoints(this WebApplication app)
    {
        // Web: list all mushaf editions with CDN URLs (no auth needed)
        app.MapGet("/api/quran/mushafs", () =>
            Results.Ok(Editions));

        // Web: single edition config
        app.MapGet("/api/quran/mushafs/{editionId}", (string editionId) =>
        {
            var edition = Editions.FirstOrDefault(e => e.Id == editionId);
            return edition is null
                ? Results.NotFound(new { error = $"Unknown edition: {editionId}" })
                : Results.Ok(edition);
        });

        // Mobile: presigned zip download URL
        app.MapPost("/api/quran/mushaf-url", async ([FromBody] MushafUrlReq req, StorageService storage) =>
        {
            if (string.IsNullOrWhiteSpace(req.MushafId) || !ValidMushafs.Contains(req.MushafId))
                return Results.BadRequest(new { error = "Invalid or missing mushafId." });

            var key = $"assets/al-quran/mushafs/{req.MushafId}.zip";
            var url = storage.GetPresignedUrl(key, 3600);
            var size = await storage.GetFileSizeAsync(key);
            return Results.Ok(new { url, sizeBytes = size });
        });

        app.MapPost("/api/quran/tafsir-url", async ([FromBody] TafsirUrlReq req, StorageService storage) =>
        {
            if (string.IsNullOrWhiteSpace(req.TafsirId) || !ValidTafsirs.Contains(req.TafsirId))
                return Results.BadRequest(new { error = "Invalid or missing tafsirId." });

            var key = $"assets/al-quran/tafsirs/{req.TafsirId}.json";
            var url = storage.GetPresignedUrl(key, 3600);
            var size = await storage.GetFileSizeAsync(key);
            return Results.Ok(new { url, sizeBytes = size });
        });

        app.MapPost("/api/quran/db-url", async ([FromBody] DbUrlReq req, StorageService storage) =>
        {
            if (string.IsNullOrWhiteSpace(req.DbName) || !ValidDbs.Contains(req.DbName))
                return Results.BadRequest(new { error = "Invalid or missing dbName." });

            var key = $"assets/db/{req.DbName}.sqlite3";
            var url = storage.GetPresignedUrl(key, 3600);
            var size = await storage.GetFileSizeAsync(key);
            return Results.Ok(new { url, sizeBytes = size });
        });

        app.MapPost("/api/quran/sura-audio-urls", async ([FromBody] SuraAudioUrlsReq req, StorageService storage) =>
        {
            if (string.IsNullOrWhiteSpace(req.ReciterId))
                return Results.BadRequest(new { error = "Missing required parameter: reciterId." });

            if (req.Sura < 1 || req.Sura > 114)
                return Results.BadRequest(new { error = "Invalid sura number. Must be 1–114." });

            var totalAyahs = AyahCounts[req.Sura - 1];

            // Generate all presigned URLs (CPU-only, no network) and kick off size requests in parallel
            var keys = Enumerable.Range(1, totalAyahs)
                .Select(a => $"assets/al-quran/qirats/{req.ReciterId}/{req.Sura}/{a}.mp3")
                .ToList();

            var urls = keys.Select(k => storage.GetPresignedUrl(k, 300)).ToList();
            var sizes = await Task.WhenAll(keys.Select(k => storage.GetFileSizeAsync(k)));

            var totalBytes = sizes.Sum();
            var totalMb = Math.Round(totalBytes / (1024.0 * 1024.0), 2);

            return Results.Ok(new
            {
                urls,
                totalAyahs,
                totalDownloadSizeBytes = totalBytes,
                totalDownloadSizeMB = totalMb,
            });
        });
    }
}

record MushafUrlReq(string MushafId);
record TafsirUrlReq(string TafsirId);
record DbUrlReq(string DbName);
record SuraAudioUrlsReq(string ReciterId, int Sura);
