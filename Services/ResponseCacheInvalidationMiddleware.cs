using Microsoft.AspNetCore.OutputCaching;

namespace IslamiJindegiApi.Services;

/// <summary>
/// Evicts public response-cache groups after a successful admin write. Keeping
/// this in middleware prevents cache invalidation from being missed by one of
/// the many CRUD actions.
/// </summary>
public class ResponseCacheInvalidationMiddleware(RequestDelegate next)
{
    static readonly string[] ContentTags =
    [
        "books", "articles", "bayans", "masails", "malfuzats", "duas",
        "madrasahs", "pages", "authors", "categories", "news", "namaz-times",
    ];

    public async Task InvokeAsync(HttpContext context, IOutputCacheStore cache)
    {
        await next(context);

        if (HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method)
            || context.Response.StatusCode is < 200 or >= 300)
            return;

        var tags = GetTags(context.Request.Path);
        foreach (var tag in tags)
            await cache.EvictByTagAsync(tag, context.RequestAborted);
    }

    static IEnumerable<string> GetTags(PathString path) => path.Value switch
    {
        var p when p?.StartsWith("/api/books", StringComparison.OrdinalIgnoreCase) == true
            || p?.StartsWith("/api/chapters", StringComparison.OrdinalIgnoreCase) == true
            || p?.StartsWith("/api/subchapters", StringComparison.OrdinalIgnoreCase) == true => ["books"],
        var p when p?.StartsWith("/api/articles", StringComparison.OrdinalIgnoreCase) == true => ["articles"],
        var p when p?.StartsWith("/api/bayan", StringComparison.OrdinalIgnoreCase) == true => ["bayans"],
        var p when p?.StartsWith("/api/masail", StringComparison.OrdinalIgnoreCase) == true => ["masails"],
        var p when p?.StartsWith("/api/malfuzat", StringComparison.OrdinalIgnoreCase) == true => ["malfuzats"],
        var p when p?.StartsWith("/api/dua", StringComparison.OrdinalIgnoreCase) == true => ["duas"],
        var p when p?.StartsWith("/api/madrasahs", StringComparison.OrdinalIgnoreCase) == true => ["madrasahs"],
        var p when p?.StartsWith("/api/pages", StringComparison.OrdinalIgnoreCase) == true => ["pages"],
        var p when p?.StartsWith("/api/categories", StringComparison.OrdinalIgnoreCase) == true => ContentTags,
        var p when p?.StartsWith("/api/authors", StringComparison.OrdinalIgnoreCase) == true
            => ["authors", "books", "articles", "bayans", "masails", "malfuzats"],
        var p when p?.StartsWith("/api/news", StringComparison.OrdinalIgnoreCase) == true => ["news"],
        var p when p?.StartsWith("/api/namaz-times", StringComparison.OrdinalIgnoreCase) == true => ["namaz-times"],
        var p when p?.StartsWith("/api/hijri/sightings", StringComparison.OrdinalIgnoreCase) == true => ["hijri"],
        _ => [],
    };
}
