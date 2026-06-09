using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using IslamiJindegiApi.Services;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class MediaEndpoints
{
    static readonly HashSet<string> AllowedMimeTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif",
        "audio/mpeg", "audio/mp3", "audio/mp4", "audio/ogg", "audio/wav", "audio/webm", "audio/x-m4a",
        "application/pdf"
    ];

    const long MaxSizeBytes = 200 * 1024 * 1024; // 200 MB

    static string GetType(string mimeType) => mimeType switch
    {
        var m when m.StartsWith("image/") => "image",
        var m when m.StartsWith("audio/") => "audio",
        _ => "document"
    };

    public static void MapMediaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/media");

        group.MapGet("/", async (
            AppDbContext db,
            int page = 1,
            int pageSize = 24,
            string? search = null,
            string? type = null) =>
        {
            var query = db.Medias.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.FileName.Contains(search));

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(m => m.Type == type);

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new PagedResult<MediaResponse>(data.Select(ToResponse), total, page, pageSize));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var media = await db.Medias.FindAsync(id);
            return media is null ? Results.NotFound() : Results.Ok(ToResponse(media));
        });

        group.MapPost("/upload", async (IFormFile file, AppDbContext db, StorageService storage) =>
        {
            if (!AllowedMimeTypes.Contains(file.ContentType))
                return Results.BadRequest($"File type '{file.ContentType}' is not allowed.");

            if (file.Length > MaxSizeBytes)
                return Results.BadRequest("File must be under 200 MB.");

            await using var stream = file.OpenReadStream();
            var (key, url) = await storage.UploadWithKeyAsync(stream, file.FileName, file.ContentType);

            var media = new Media
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                StorageKey = key,
                Url = url,
                Type = GetType(file.ContentType),
                MimeType = file.ContentType,
                Size = file.Length,
                Description = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            db.Medias.Add(media);
            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(media));
        }).DisableAntiforgery();

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, StorageService storage) =>
        {
            var media = await db.Medias.FindAsync(id);
            if (media is null) return Results.NotFound();

            try { await storage.DeleteAsync(media.StorageKey); }
            catch { /* log but don't block DB delete if storage fails */ }

            db.Medias.Remove(media);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static MediaResponse ToResponse(Media m) => new(
        m.Id, m.FileName, m.Url, m.Type, m.MimeType, m.Size,
        m.Width, m.Height, m.Description, m.CreatedAt, m.UpdatedAt);
}
