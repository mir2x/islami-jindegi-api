using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class CategoryService(AppDbContext db) : ICategoryService
{
    public async Task<IEnumerable<CategoryResponse>> GetAllAsync()
    {
        var categories = await db.Categories
            .Include(c => c.Children)
            .Where(c => c.ParentId == null)
            .OrderBy(c => c.Position)
            .ToListAsync();
        return categories.Select(Mappers.ToCategoryResponse);
    }

    /// <summary>
    /// Paginated top-level categories for the admin list, each with its children nested.
    /// Only roots are paged/sorted; children always come back with their parent, ordered by
    /// position. GetAllAsync stays unpaged because the filter dropdowns need the whole tree.
    /// </summary>
    public async Task<PagedResult<CategoryResponse>> GetPagedAsync(int page, int pageSize, string? search, string? sort)
    {
        var query = db.Categories
            .Include(c => c.Children)
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
            _ => query.OrderBy(c => c.Position),
        };

        var total = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<CategoryResponse>(data.Select(Mappers.ToCategoryResponse), total, page, pageSize);
    }

    public async Task<CategoryResponse?> GetByIdAsync(Guid id)
    {
        var category = await db.Categories
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);
        return category is null ? null : Mappers.ToCategoryResponse(category);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest req)
    {
        var position = req.Position ?? (await db.Categories
            .Where(c => c.ParentId == req.ParentId)
            .MaxAsync(c => (int?)c.Position) ?? 0) + 1;

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Position = position,
            ParentId = req.ParentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return Mappers.ToCategoryResponse(category);
    }

    public async Task<CategoryResponse?> UpdateAsync(Guid id, UpdateCategoryRequest req)
    {
        var category = await db.Categories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return null;

        category.Title = req.Title;
        category.ParentId = req.ParentId;
        if (req.Position.HasValue) category.Position = req.Position.Value;
        category.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Mappers.ToCategoryResponse(category);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return false;
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return true;
    }
}
