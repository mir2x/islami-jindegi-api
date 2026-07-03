using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController(IMediaService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 24,
        [FromQuery] string? search = null, [FromQuery] string? type = null)
        => Ok(await service.GetListAsync(page, pageSize, search, type));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        try
        {
            var result = await service.UploadAsync(file);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchMediaRequest req)
    {
        var result = await service.PatchAsync(id, req.FileName, req.Url);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await service.DeleteAsync(id) ? NoContent() : NotFound();
}

public record PatchMediaRequest(string? FileName, string? Url);
