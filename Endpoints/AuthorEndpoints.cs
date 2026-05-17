using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class AuthorEndpoints
{
    public static void MapAuthorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/authors");

        group.MapGet("/", async (AppDbContext db, int page = 1, int pageSize = 10, string? search = null) =>
        {
            var query = db.Authors.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.Name.Contains(search));

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(a => a.Position)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => ToResponse(a))
                .ToListAsync();

            return Results.Ok(new PagedResult<AuthorResponse>(data, total, page, pageSize));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var author = await db.Authors.FindAsync(id);
            return author is null ? Results.NotFound() : Results.Ok(ToResponse(author));
        });

        group.MapPost("/", async (CreateAuthorRequest req, AppDbContext db) =>
        {
            var position = req.Position ?? (await db.Authors.MaxAsync(a => (int?)a.Position) ?? 0) + 1;
            var author = new Author
            {
                Id = Guid.NewGuid(),
                Name = req.Name,
                Info = req.Info,
                Position = position,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Authors.Add(author);
            await db.SaveChangesAsync();
            return Results.Created($"/api/authors/{author.Id}", ToResponse(author));
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateAuthorRequest req, AppDbContext db) =>
        {
            var author = await db.Authors.FindAsync(id);
            if (author is null) return Results.NotFound();

            author.Name = req.Name;
            author.Info = req.Info;
            if (req.Position.HasValue) author.Position = req.Position.Value;
            author.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(author));
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var author = await db.Authors.FindAsync(id);
            if (author is null) return Results.NotFound();

            db.Authors.Remove(author);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    public static AuthorResponse ToResponse(Author a) =>
        new(a.Id, a.Name, a.Info, a.Position, a.CreatedAt, a.UpdatedAt);
}
