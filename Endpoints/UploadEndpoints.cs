using IslamiJindegiApi.Services;

namespace IslamiJindegiApi.Endpoints;

public static class UploadEndpoints
{
    static readonly HashSet<string> AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    static readonly HashSet<string> AllowedDocumentTypes = ["application/pdf"];
    const long MaxImageBytes = 10 * 1024 * 1024;    // 10 MB
    const long MaxDocumentBytes = 100 * 1024 * 1024; // 100 MB

    public static void MapUploadEndpoints(this WebApplication app)
    {
        app.MapPost("/api/upload/image", async (IFormFile file, StorageService storage) =>
        {
            if (!AllowedImageTypes.Contains(file.ContentType))
                return Results.BadRequest("Only JPEG, PNG, WebP, and GIF images are allowed.");

            if (file.Length > MaxImageBytes)
                return Results.BadRequest("Image must be under 10 MB.");

            await using var stream = file.OpenReadStream();
            var url = await storage.UploadAsync(stream, file.FileName, file.ContentType);
            return Results.Ok(new { url });
        }).DisableAntiforgery();

        app.MapPost("/api/upload/document", async (IFormFile file, StorageService storage) =>
        {
            if (!AllowedDocumentTypes.Contains(file.ContentType))
                return Results.BadRequest("Only PDF documents are allowed.");

            if (file.Length > MaxDocumentBytes)
                return Results.BadRequest("Document must be under 100 MB.");

            await using var stream = file.OpenReadStream();
            var url = await storage.UploadAsync(stream, file.FileName, file.ContentType);
            return Results.Ok(new { url });
        }).DisableAntiforgery();
    }
}
