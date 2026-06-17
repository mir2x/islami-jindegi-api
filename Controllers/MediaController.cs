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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await service.DeleteAsync(id) ? NoContent() : NotFound();
}
