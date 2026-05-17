using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/categories");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var categories = await db.Categories
                .Include(c => c.Children)
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.Position)
                .ToListAsync();

            return Results.Ok(categories.Select(ToResponse));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var category = await db.Categories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

            return category is null ? Results.NotFound() : Results.Ok(ToResponse(category));
        });

        group.MapPost("/", async (CreateCategoryRequest req, AppDbContext db) =>
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
            return Results.Created($"/api/categories/{category.Id}", ToResponse(category));
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateCategoryRequest req, AppDbContext db) =>
        {
            var category = await db.Categories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == id);
            if (category is null) return Results.NotFound();

            category.Title = req.Title;
            category.ParentId = req.ParentId;
            if (req.Position.HasValue) category.Position = req.Position.Value;
            category.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(category));
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var category = await db.Categories.FindAsync(id);
            if (category is null) return Results.NotFound();

            db.Categories.Remove(category);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    public static CategoryResponse ToResponse(Category c) =>
        new(c.Id, c.Title, c.Position, c.ParentId,
            c.Children.OrderBy(ch => ch.Position).Select(ToResponse).ToList(),
            c.CreatedAt, c.UpdatedAt);
}
