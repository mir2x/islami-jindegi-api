using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IslamiJindegiApi.Services;

public class CategoryService(AppDbContext db) : ICategoryService
{
    // Table and key column for every content type a category can be attached to. Used by the
    // merge and usage paths, which have to touch all six join tables. These are compile-time
    // constants, never user input, so interpolating them into SQL is safe.
    static readonly (string Module, string Table, string Column)[] JoinTables =
    [
        (ContentModules.Book,     "book_categories",     "BooksId"),
        (ContentModules.Bayan,    "bayan_categories",    "BayansId"),
        (ContentModules.Malfuzat, "malfuzat_categories", "MalfuzatsId"),
        (ContentModules.Masail,   "masail_categories",   "MasailsId"),
        (ContentModules.Dua,      "dua_categories",      "DuasId"),
        (ContentModules.Article,  "article_categories",  "ArticlesId"),
    ];

    public async Task<IEnumerable<CategoryResponse>> GetAllAsync()
    {
        var categories = await db.Categories
            .AsNoTracking()
            .Include(c => c.Children)
            .Include(c => c.Modules)
            .Where(c => c.ParentId == null)
            .OrderBy(c => c.Position)
            .ToListAsync();
        return categories.Select(Mappers.ToAdminCategoryResponse);
    }

    /// <summary>
    /// Paginated top-level categories for the admin list, each with its children nested.
    /// Only roots are paged/sorted; children always come back with their parent, ordered by
    /// position. GetAllAsync stays unpaged because the filter dropdowns need the whole tree.
    /// </summary>
    public async Task<PagedResult<CategoryResponse>> GetPagedAsync(int page, int pageSize, string? search, string? sort)
    {
        var query = db.Categories
            .AsNoTracking()
            .Include(c => c.Children)
            .Include(c => c.Modules)
            .Where(c => c.ParentId == null);

        // Match a root by its own title or by any of its children's, so searching for a
        // subcategory still surfaces the parent it lives under.
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search) || c.Children.Any(ch => ch.Title.Contains(search)));

        query = sort switch
        {
            "position_desc" => query.OrderByDescending(c => c.Position),
            "position_asc" => query.OrderBy(c => c.Position),
            "title_asc" => query.OrderBy(c => c.Title),
            "title_desc" => query.OrderByDescending(c => c.Title),
            "subs_asc" => query.OrderBy(c => c.Children.Count).ThenBy(c => c.Position),
            "subs_desc" => query.OrderByDescending(c => c.Children.Count).ThenBy(c => c.Position),
            "modules_asc" => query.OrderBy(c => c.Modules.Count).ThenBy(c => c.Position),
            "modules_desc" => query.OrderByDescending(c => c.Modules.Count).ThenBy(c => c.Position),
            _ => query.OrderBy(c => c.Position),
        };

        var total = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<CategoryResponse>(data.Select(Mappers.ToAdminCategoryResponse), total, page, pageSize);
    }

    public async Task<CategoryResponse?> GetByIdAsync(Guid id)
    {
        var category = await db.Categories
            .AsNoTracking()
            .Include(c => c.Children)
            .Include(c => c.Modules)
            .FirstOrDefaultAsync(c => c.Id == id);
        return category is null ? null : Mappers.ToAdminCategoryResponse(category);
    }

    public async Task<CategoryWriteResult> CreateAsync(CreateCategoryRequest req)
    {
        if (InvalidModules(req.Modules) is { } bad)
            return new(CategoryWriteStatus.InvalidModule, Message: bad);

        var position = req.Position ?? (await db.Categories
            .Where(c => c.ParentId == req.ParentId)
            .MaxAsync(c => (int?)c.Position) ?? 0) + 1;

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Title = req.Title.Trim(),
            Position = position,
            ParentId = req.ParentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Categories.Add(category);

        foreach (var module in req.Modules ?? [])
            category.Modules.Add(new CategoryModule
            {
                CategoryId = category.Id,
                Module = module,
                Position = await NextPositionAsync(module)
            });

        return await SaveAsync(category);
    }

    public async Task<CategoryWriteResult> UpdateAsync(Guid id, UpdateCategoryRequest req)
    {
        if (InvalidModules(req.Modules) is { } bad)
            return new(CategoryWriteStatus.InvalidModule, Message: bad);

        var category = await db.Categories
            .Include(c => c.Children)
            .Include(c => c.Modules)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return new(CategoryWriteStatus.NotFound);

        category.Title = req.Title.Trim();
        category.ParentId = req.ParentId;
        if (req.Position.HasValue) category.Position = req.Position.Value;
        category.UpdatedAt = DateTime.UtcNow;

        // A null Modules list means "not editing membership" — only an explicit list rewrites it,
        // so callers that never send the field (content forms) cannot wipe it by omission.
        if (req.Modules is not null)
        {
            foreach (var gone in category.Modules.Where(m => !req.Modules.Contains(m.Module)).ToList())
                category.Modules.Remove(gone);

            foreach (var module in req.Modules.Where(m => category.Modules.All(x => x.Module != m)))
                category.Modules.Add(new CategoryModule
                {
                    CategoryId = category.Id,
                    Module = module,
                    Position = await NextPositionAsync(module)
                });
        }

        return await SaveAsync(category);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return false;
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CategoryUsage>> GetUsageAsync(Guid id)
    {
        var usage = new List<CategoryUsage>();
        foreach (var (module, table, _) in JoinTables)
        {
            var count = await db.Database
                .SqlQueryRaw<int>($$"""SELECT count(*)::int AS "Value" FROM {{table}} WHERE "CategoriesId" = {0}""", id)
                .SingleAsync();
            if (count > 0) usage.Add(new CategoryUsage(module, count));
        }
        return usage;
    }

    /// <summary>
    /// Moves every content link and module membership from one category onto another, then
    /// deletes the source. This is the operation the admin previously lacked — staff were
    /// consolidating categories by RENAMING one to another's title, which moves no content and
    /// produced duplicate titles with the content split between them.
    /// </summary>
    public async Task<CategoryWriteResult> MergeAsync(Guid sourceId, Guid targetId)
    {
        if (sourceId == targetId)
            return new(CategoryWriteStatus.InvalidTarget, Message: "A category cannot be merged into itself.");

        var source = await db.Categories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == sourceId);
        if (source is null) return new(CategoryWriteStatus.NotFound);
        if (!await db.Categories.AnyAsync(c => c.Id == targetId))
            return new(CategoryWriteStatus.InvalidTarget, Message: "Target category not found.");

        await using var tx = await db.Database.BeginTransactionAsync();

        foreach (var (_, table, column) in JoinTables)
        {
            // ON CONFLICT DO NOTHING is required, not defensive: content can already carry both
            // categories, and the join tables have a composite primary key.
            await db.Database.ExecuteSqlRawAsync(
                $$"""
                  INSERT INTO {{table}} ("{{column}}", "CategoriesId")
                  SELECT "{{column}}", {1} FROM {{table}} WHERE "CategoriesId" = {0}
                  ON CONFLICT DO NOTHING
                  """, sourceId, targetId);
            await db.Database.ExecuteSqlRawAsync(
                $$"""DELETE FROM {{table}} WHERE "CategoriesId" = {0}""", sourceId);
        }

        // Carry over any module the target is not already in, keeping the target's own position
        // where both are members.
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO category_modules ("CategoryId", "Module", "Position")
            SELECT {1}, s."Module", s."Position" FROM category_modules s
            WHERE s."CategoryId" = {0}
              AND NOT EXISTS (SELECT 1 FROM category_modules t
                              WHERE t."CategoryId" = {1} AND t."Module" = s."Module")
            ON CONFLICT DO NOTHING
            """, sourceId, targetId);

        // Children would be orphaned by the delete, so they follow the content.
        await db.Database.ExecuteSqlRawAsync(
            """UPDATE "Categories" SET "ParentId" = {1} WHERE "ParentId" = {0}""", sourceId, targetId);

        await db.Database.ExecuteSqlRawAsync(
            """DELETE FROM "Categories" WHERE "Id" = {0}""", sourceId);

        await tx.CommitAsync();

        var target = await db.Categories.AsNoTracking()
            .Include(c => c.Children).Include(c => c.Modules)
            .FirstAsync(c => c.Id == targetId);
        return new(CategoryWriteStatus.Ok, Mappers.ToAdminCategoryResponse(target));
    }

    /// <summary>Rewrites one module's category order to 1..N in the order given.</summary>
    public async Task<CategoryWriteResult> ReorderAsync(ReorderCategoriesRequest req)
    {
        if (!ContentModules.All.Contains(req.Module))
            return new(CategoryWriteStatus.InvalidModule, Message: $"Unknown module '{req.Module}'.");

        var members = await db.CategoryModules.Where(m => m.Module == req.Module).ToListAsync();
        var byId = members.ToDictionary(m => m.CategoryId);

        var position = 1;
        foreach (var id in req.CategoryIds)
            if (byId.TryGetValue(id, out var member))
                member.Position = position++;

        // Anything the client did not list keeps its relative order after the listed ones, so a
        // stale page cannot silently drop a category to position 0.
        foreach (var leftover in members.Where(m => !req.CategoryIds.Contains(m.CategoryId)).OrderBy(m => m.Position))
            leftover.Position = position++;

        await db.SaveChangesAsync();
        return new(CategoryWriteStatus.Ok);
    }

    async Task<int> NextPositionAsync(string module) =>
        (await db.CategoryModules.Where(m => m.Module == module)
            .MaxAsync(m => (int?)m.Position) ?? 0) + 1;

    static string? InvalidModules(List<string>? modules)
    {
        var unknown = (modules ?? []).Where(m => !ContentModules.All.Contains(m)).ToList();
        return unknown.Count == 0 ? null : $"Unknown module(s): {string.Join(", ", unknown)}.";
    }

    /// <summary>
    /// Saves and turns a unique-title violation into a result the controller can report as 409.
    /// The index is on the NORMALISED title, so this also catches names differing only by
    /// apostrophe style or Bengali Unicode composition — which is exactly how the duplicates
    /// got in before.
    /// </summary>
    async Task<CategoryWriteResult> SaveAsync(Category category)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
        {
            return new(CategoryWriteStatus.DuplicateTitle,
                Message: $"A category named \"{category.Title}\" already exists. Merge into it instead of renaming.");
        }

        var saved = await db.Categories.AsNoTracking()
            .Include(c => c.Children).Include(c => c.Modules)
            .FirstAsync(c => c.Id == category.Id);
        return new(CategoryWriteStatus.Ok, Mappers.ToAdminCategoryResponse(saved));
    }
}
