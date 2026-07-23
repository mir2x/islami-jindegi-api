using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadController(StorageService storage) : ControllerBase
{
    static readonly HashSet<string> AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    static readonly HashSet<string> AllowedDocumentTypes = ["application/pdf"];
    const long MaxImageBytes = 10 * 1024 * 1024;
    const long MaxDocumentBytes = 100 * 1024 * 1024;

    [Authorize]
    [HttpPost("image")]
    [DisableRequestSizeLimit]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (!AllowedImageTypes.Contains(file.ContentType))
            return BadRequest("Only JPEG, PNG, WebP, and GIF images are allowed.");
        if (file.Length > MaxImageBytes)
            return BadRequest("Image must be under 10 MB.");

        await using var stream = file.OpenReadStream();
        var url = await storage.UploadAsync(stream, file.FileName, file.ContentType);
        return Ok(new { url });
    }

    [Authorize]
    [HttpPost("document")]
    [DisableRequestSizeLimit]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (!AllowedDocumentTypes.Contains(file.ContentType))
            return BadRequest("Only PDF documents are allowed.");
        if (file.Length > MaxDocumentBytes)
            return BadRequest("Document must be under 100 MB.");

        await using var stream = file.OpenReadStream();
        var url = await storage.UploadAsync(stream, file.FileName, file.ContentType);
        return Ok(new { url });
    }
}
