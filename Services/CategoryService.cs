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
