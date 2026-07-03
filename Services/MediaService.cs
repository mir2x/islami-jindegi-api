using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class MediaService(AppDbContext db, StorageService storage) : IMediaService
{
    static readonly HashSet<string> AllowedMimeTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif",
        "audio/mpeg", "audio/mp3", "audio/mp4", "audio/ogg", "audio/wav", "audio/webm", "audio/x-m4a",
        "application/pdf"
    ];

    const long MaxSizeBytes = 200 * 1024 * 1024;

    static string GetType(string mimeType) => mimeType switch
    {
        var m when m.StartsWith("image/") => "image",
        var m when m.StartsWith("audio/") => "audio",
        _ => "document"
    };

    public async Task<PagedResult<MediaResponse>> GetListAsync(int page, int pageSize, string? search, string? type)
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

        return new PagedResult<MediaResponse>(data.Select(Mappers.ToMediaResponse), total, page, pageSize);
    }

    public async Task<MediaResponse?> GetByIdAsync(Guid id)
    {
        var media = await db.Medias.FindAsync(id);
        return media is null ? null : Mappers.ToMediaResponse(media);
    }

    public async Task<MediaResponse> UploadAsync(IFormFile file)
    {
        if (!AllowedMimeTypes.Contains(file.ContentType))
            throw new InvalidOperationException($"File type '{file.ContentType}' is not allowed.");
        if (file.Length > MaxSizeBytes)
            throw new InvalidOperationException("File must be under 200 MB.");

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
        return Mappers.ToMediaResponse(media);
    }

    public async Task<MediaResponse?> PatchAsync(Guid id, string? fileName, string? url)
    {
        var media = await db.Medias.FindAsync(id);
        if (media is null) return null;

        if (fileName is not null) media.FileName = fileName.Trim();
        if (url is not null) media.Url = url.Trim();
        media.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Mappers.ToMediaResponse(media);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var media = await db.Medias.FindAsync(id);
        if (media is null) return false;

        try { await storage.DeleteAsync(media.StorageKey); }
        catch { /* don't block DB delete if storage fails */ }

        db.Medias.Remove(media);
        await db.SaveChangesAsync();
        return true;
    }
}
