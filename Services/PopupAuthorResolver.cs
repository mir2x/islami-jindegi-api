using IslamiJindegiApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IslamiJindegiApi.Services;

/// Resolves the single author whose malfuzat feeds the app's daily popup.
///
/// The app used to hardcode this author's primary key. That key came from the
/// legacy Ruby database, and the .NET migration mints a fresh `Guid.NewGuid()`
/// for any author name it cannot match (see `MigrateModuleAuthors`) while
/// `CleanupDuplicateAuthors` deletes merged rows outright. So the identity is
/// not stable across migrations: every re-run silently orphaned the app's GUID
/// and killed the popup until someone shipped a new store build.
///
/// Identity therefore lives here, resolved by NAME at request time, and the
/// lookup deliberately degrades through progressively looser matches rather
/// than ever falling back to a different author — the popup is specifically
/// this shaykh's malfuzat, so returning somebody else would be worse than
/// returning nothing.
public sealed class PopupAuthorResolver(AppDbContext db, IMemoryCache cache, ILogger<PopupAuthorResolver> logger)
{
    /// Canonical name in the new system, matching `MigrateDataCommand`'s author maps.
    public const string DefaultAuthorName = "মুফতী মনসূরুল হক সাহেব";

    /// The distinctive part of the name, used as the last-resort match so that
    /// added or reworded honorifics ("হযরতওয়ালা …", "… রহ.") cannot lose him.
    const string NameFragment = "মনসূরুল হক";

    const string CacheKey = "popup-author-id";

    /// Short enough that renaming an author in the admin takes effect on its
    /// own, long enough that the daily-popup path is one query, not two.
    static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(10);

    public async Task<Guid?> ResolveAsync()
    {
        if (cache.TryGetValue(CacheKey, out Guid? cached))
            return cached;

        var resolved = await LookupAsync();

        // Negative results are cached too, otherwise a misconfiguration turns
        // every app launch into a full scan of the Authors table.
        cache.Set(CacheKey, resolved, CacheFor);
        return resolved;
    }

    async Task<Guid?> LookupAsync()
    {
        // 1. Explicit override. The escape hatch: if the name ever changes in a
        //    way the matches below cannot follow, set this and restart — no code
        //    deploy, and crucially no Play Store release.
        var configuredId = Environment.GetEnvironmentVariable("MALFUZAT_POPUP_AUTHOR_ID");
        if (Guid.TryParse(configuredId, out var overrideId))
        {
            if (await db.Authors.AnyAsync(a => a.Id == overrideId))
                return overrideId;
            logger.LogError(
                "MALFUZAT_POPUP_AUTHOR_ID={AuthorId} does not match any author; falling back to name lookup.",
                overrideId);
        }

        var name = Environment.GetEnvironmentVariable("MALFUZAT_POPUP_AUTHOR_NAME") ?? DefaultAuthorName;

        // 2. Exact match on the canonical name.
        var exact = await db.Authors
            .Where(a => a.Name == name)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync();
        if (exact is not null) return exact;

        // 3. Zero-width-character tolerant match. The corpus genuinely contains
        //    names differing only by an invisible ZWJ — `MigrateDataCommand`
        //    carries a map entry for exactly that on another author. Compared in
        //    memory because the normalisation has no SQL translation; the
        //    Authors table is small (single digits), so this costs nothing.
        var candidates = await db.Authors
            .Select(a => new { a.Id, a.Name })
            .ToListAsync();

        var target = Normalize(name);
        var normalized = candidates.FirstOrDefault(a => Normalize(a.Name) == target);
        if (normalized is not null)
        {
            logger.LogWarning(
                "Popup author matched only after normalisation: stored {Stored:l} vs configured {Configured:l}.",
                normalized.Name, name);
            return normalized.Id;
        }

        // 4. Last resort: the distinctive fragment of the name, which survives
        //    honorifics being added or dropped.
        var fragment = Normalize(NameFragment);
        var partial = candidates
            .Where(a => Normalize(a.Name).Contains(fragment, StringComparison.Ordinal))
            .ToList();
        if (partial.Count == 1)
        {
            logger.LogWarning(
                "Popup author matched only by name fragment: stored {Stored:l} vs configured {Configured:l}.",
                partial[0].Name, name);
            return partial[0].Id;
        }

        logger.LogError(
            "Popup author {Configured:l} could not be resolved ({Matches} fragment matches). The daily malfuzat popup is disabled until this is fixed.",
            name, partial.Count);
        return null;
    }

    /// Strips the zero-width joiner/non-joiner and collapses whitespace. These
    /// are invisible in the admin UI, so a name can differ from its intended
    /// value in a way nobody can see by looking at it.
    static string Normalize(string value)
    {
        // Author names are short, but they come from the database, so do not
        // let their length drive an unbounded stack allocation.
        Span<char> buffer = value.Length <= 256 ? stackalloc char[256] : new char[value.Length];
        var length = 0;
        var lastWasSpace = false;

        foreach (var c in value)
        {
            // ZWNJ, ZWJ, zero-width space, BOM — written as escapes because
            // as literals they are invisible in the source too.
            if (c is '\u200C' or '\u200D' or '\u200B' or '\uFEFF') continue;
            if (char.IsWhiteSpace(c))
            {
                if (lastWasSpace || length == 0) continue;
                lastWasSpace = true;
                buffer[length++] = ' ';
                continue;
            }
            lastWasSpace = false;
            buffer[length++] = c;
        }

        while (length > 0 && buffer[length - 1] == ' ') length--;
        return new string(buffer[..length]);
    }
}
