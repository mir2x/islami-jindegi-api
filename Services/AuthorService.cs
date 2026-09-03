using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IslamiJindegiApi.Services;

public class AuthorService(AppDbContext db) : IAuthorService
{
    // How each module attributes content to an author. Four hold a plain foreign key on the
    // content row; books are many-to-many through a join table, so they are handled separately
    // everywhere below. These are compile-time constants, never user input, so interpolating
    // them into SQL is safe.
    static readonly (string Module, string Table)[] FkTables =
    [
        (AuthorModules.Bayan,    "Bayans"),
        (AuthorModules.Malfuzat, "Malfuzats"),
        (AuthorModules.Masail,   "Masails"),
        (AuthorModules.Article,  "Articles"),
    ];

    /// <summary>
    /// Every author, unpaged, with module memberships. The admin needs the whole list for its
    /// pickers, the merge target dropdown and the reorder screen — and it cannot get there by
    /// asking GetListAsync for a big page, because PageSizeClampFilter caps pageSize at 100 and
    /// silently truncates. Mirrors the unpaged category endpoint.
    /// </summary>
    public async Task<IEnumerable<AuthorResponse>> GetAllAsync()
    {
        var authors = await db.Authors
            .AsNoTracking()
            .Include(a => a.Modules)
            .OrderBy(a => a.Position)
            .ToListAsync();
        return authors.Select(Mappers.ToAdminAuthorResponse);
    }

    public async Task<PagedResult<AuthorResponse>> GetListAsync(int page, int pageSize, string? search, string? sort = null)
    {
        var query = db.Authors.AsNoTracking().Include(a => a.Modules).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Name.Contains(search));

        query = sort switch
        {
            "position_desc" => query.OrderByDescending(a => a.Position),
            "position_asc" => query.OrderBy(a => a.Position),
            "name_asc" => query.OrderBy(a => a.Name),
            "name_desc" => query.OrderByDescending(a => a.Name),
            "modules_asc" => query.OrderBy(a => a.Modules.Count).ThenBy(a => a.Position),
            "modules_desc" => query.OrderByDescending(a => a.Modules.Count).ThenBy(a => a.Position),
            _ => query.OrderBy(a => a.Position),
        };

        var total = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AuthorResponse>(data.Select(Mappers.ToAdminAuthorResponse), total, page, pageSize);
    }

    public async Task<AuthorResponse?> GetByIdAsync(Guid id)
    {
        var author = await db.Authors.AsNoTracking()
            .Include(a => a.Modules)
            .FirstOrDefaultAsync(a => a.Id == id);
        return author is null ? null : Mappers.ToAdminAuthorResponse(author);
    }

    public async Task<AuthorWriteResult> CreateAsync(CreateAuthorRequest req)
    {
        if (InvalidModules(req.Modules) is { } bad)
            return new(AuthorWriteStatus.InvalidModule, Message: bad);

        var position = req.Position ?? (await db.Authors.MaxAsync(a => (int?)a.Position) ?? 0) + 1;
        var author = new Author
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Info = req.Info,
            Position = position,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Authors.Add(author);

        foreach (var module in req.Modules ?? [])
            author.Modules.Add(new AuthorModule
            {
                AuthorId = author.Id,
                Module = module,
                Position = await NextPositionAsync(module)
            });

        return await SaveAsync(author);
    }

    public async Task<AuthorWriteResult> UpdateAsync(Guid id, UpdateAuthorRequest req)
    {
        if (InvalidModules(req.Modules) is { } bad)
            return new(AuthorWriteStatus.InvalidModule, Message: bad);

        var author = await db.Authors.Include(a => a.Modules).FirstOrDefaultAsync(a => a.Id == id);
        if (author is null) return new(AuthorWriteStatus.NotFound);

        author.Name = req.Name.Trim();
        author.Info = req.Info;
        if (req.Position.HasValue) author.Position = req.Position.Value;
        author.UpdatedAt = DateTime.UtcNow;

        // A null Modules list means "not editing membership" — only an explicit list rewrites it,
        // so callers that never send the field (content forms) cannot wipe it by omission.
        if (req.Modules is not null)
        {
            foreach (var gone in author.Modules.Where(m => !req.Modules.Contains(m.Module)).ToList())
                author.Modules.Remove(gone);

            foreach (var module in req.Modules.Where(m => author.Modules.All(x => x.Module != m)))
                author.Modules.Add(new AuthorModule
                {
                    AuthorId = author.Id,
                    Module = module,
                    Position = await NextPositionAsync(module)
                });
        }

        return await SaveAsync(author);
    }

    /// <summary>
    /// Refuses while the author still owns content. Bayan and Malfuzat require an author, so a
    /// delete used to cascade — one click could take thousands of published items with it, with
    /// no warning and nothing to restore from. Merge is the way to retire an author who has
    /// content; delete is only for one that never had any.
    /// </summary>
    public async Task<AuthorWriteResult> DeleteAsync(Guid id)
    {
        var author = await db.Authors.FindAsync(id);
        if (author is null) return new(AuthorWriteStatus.NotFound);

        var usage = (await GetUsageAsync(id)).ToList();
        if (usage.Count > 0)
            return new(AuthorWriteStatus.HasContent,
                Message: $"\"{author.Name}\" still has {usage.Sum(u => u.Items)} item(s) " +
                         $"({string.Join(", ", usage.Select(u => $"{u.Module}: {u.Items}"))}). " +
                         "Merge into another author instead of deleting.");

        db.Authors.Remove(author);
        await db.SaveChangesAsync();
        return new(AuthorWriteStatus.Ok);
    }

    public async Task<IEnumerable<AuthorUsage>> GetUsageAsync(Guid id)
    {
        var usage = new List<AuthorUsage>();

        var books = await db.Database
            .SqlQueryRaw<int>("""SELECT count(*)::int AS "Value" FROM book_authors WHERE "AuthorsId" = {0}""", id)
            .SingleAsync();
        if (books > 0) usage.Add(new AuthorUsage(AuthorModules.Book, books));

        foreach (var (module, table) in FkTables)
        {
            var count = await db.Database
                .SqlQueryRaw<int>($$"""SELECT count(*)::int AS "Value" FROM "{{table}}" WHERE "AuthorId" = {0}""", id)
                .SingleAsync();
            if (count > 0) usage.Add(new AuthorUsage(module, count));
        }

        return usage;
    }

    /// <summary>
    /// Moves every piece of content and module membership from one author onto another, then
    /// deletes the source. Without this the only way to consolidate two authors is to rename one,
    /// which moves no content — that is how "মুফতী মনসূরুল হক সাহেব" came to exist twice, with the
    /// bayan and article on one row and the book, malfuzat and masail on the other.
    /// </summary>
    public async Task<AuthorWriteResult> MergeAsync(Guid sourceId, Guid targetId)
    {
        if (sourceId == targetId)
            return new(AuthorWriteStatus.InvalidTarget, Message: "An author cannot be merged into itself.");

        if (!await db.Authors.AnyAsync(a => a.Id == sourceId)) return new(AuthorWriteStatus.NotFound);
        if (!await db.Authors.AnyAsync(a => a.Id == targetId))
            return new(AuthorWriteStatus.InvalidTarget, Message: "Target author not found.");

        await using var tx = await db.Database.BeginTransactionAsync();

        // ON CONFLICT DO NOTHING is required, not defensive: a book can already carry both
        // authors, and the join table has a composite primary key.
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO book_authors ("BooksId", "AuthorsId")
            SELECT "BooksId", {1} FROM book_authors WHERE "AuthorsId" = {0}
            ON CONFLICT DO NOTHING
            """, sourceId, targetId);
        await db.Database.ExecuteSqlRawAsync(
            """DELETE FROM book_authors WHERE "AuthorsId" = {0}""", sourceId);

        // UpdatedAt is bumped deliberately: the offline sync endpoints are delta queries on
        // UpdatedAt and they embed the whole author object, so without this every phone would
        // keep serving the deleted author's id and name for the content it already holds.
        foreach (var (_, table) in FkTables)
            await db.Database.ExecuteSqlRawAsync(
                $$"""UPDATE "{{table}}" SET "AuthorId" = {1}, "UpdatedAt" = now() WHERE "AuthorId" = {0}""",
                sourceId, targetId);

        // Carry over any module the target is not already in, keeping the target's own position
        // where both are members.
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO author_modules ("AuthorId", "Module", "Position")
            SELECT {1}, s."Module", s."Position" FROM author_modules s
            WHERE s."AuthorId" = {0}
              AND NOT EXISTS (SELECT 1 FROM author_modules t
                              WHERE t."AuthorId" = {1} AND t."Module" = s."Module")
            ON CONFLICT DO NOTHING
            """, sourceId, targetId);

        await db.Database.ExecuteSqlRawAsync(
            """DELETE FROM "Authors" WHERE "Id" = {0}""", sourceId);

        await tx.CommitAsync();

        var target = await db.Authors.AsNoTracking()
            .Include(a => a.Modules)
            .FirstAsync(a => a.Id == targetId);
        return new(AuthorWriteStatus.Ok, Mappers.ToAdminAuthorResponse(target));
    }

    /// <summary>Rewrites one module's author order to 1..N in the order given.</summary>
    public async Task<AuthorWriteResult> ReorderAsync(ReorderAuthorsRequest req)
    {
        if (!AuthorModules.All.Contains(req.Module))
            return new(AuthorWriteStatus.InvalidModule, Message: $"Unknown module '{req.Module}'.");

        var members = await db.AuthorModules.Where(m => m.Module == req.Module).ToListAsync();
        var byId = members.ToDictionary(m => m.AuthorId);

        var position = 1;
        foreach (var id in req.AuthorIds)
            if (byId.TryGetValue(id, out var member))
                member.Position = position++;

        // Anything the client did not list keeps its relative order after the listed ones, so a
        // stale page cannot silently drop an author to position 0.
        foreach (var leftover in members.Where(m => !req.AuthorIds.Contains(m.AuthorId)).OrderBy(m => m.Position))
            leftover.Position = position++;

        await db.SaveChangesAsync();
        return new(AuthorWriteStatus.Ok);
    }

    async Task<int> NextPositionAsync(string module) =>
        (await db.AuthorModules.Where(m => m.Module == module)
            .MaxAsync(m => (int?)m.Position) ?? 0) + 1;

    static string? InvalidModules(List<string>? modules)
    {
        var unknown = (modules ?? []).Where(m => !AuthorModules.All.Contains(m)).ToList();
        return unknown.Count == 0 ? null : $"Unknown module(s): {string.Join(", ", unknown)}.";
    }

    /// <summary>
    /// Saves and turns a unique-name violation into a result the controller can report as 409.
    /// The index is on the NORMALISED name, so this also catches names differing only by an
    /// invisible zero-width joiner or Bengali Unicode composition — which is exactly how two of
    /// the three duplicate authors got in.
    /// </summary>
    async Task<AuthorWriteResult> SaveAsync(Author author)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
        {
            return new(AuthorWriteStatus.DuplicateName,
                Message: $"An author named \"{author.Name}\" already exists. Merge into them instead of renaming.");
        }

        var saved = await db.Authors.AsNoTracking()
            .Include(a => a.Modules)
            .FirstAsync(a => a.Id == author.Id);
        return new(AuthorWriteStatus.Ok, Mappers.ToAdminAuthorResponse(saved));
    }
}
