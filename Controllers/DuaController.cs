using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/dua")]
public class DuaController(IDuaService service) : ControllerBase
{
    [HttpGet]
    [OutputCache(Duration = 120, Tags = ["duas", "categories"])]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null, [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? published = null, [FromQuery] bool? hasAudio = null,
        [FromQuery] bool? offlineAvailable = null, [FromQuery] string? sort = null)
        => Ok(await service.GetListAsync(page, pageSize, search, categoryId, published, hasAudio, offlineAvailable, sort));

    // Cached because every client that has no watermark yet asks for the
    // same full-corpus response. Without this, each device re-runs the most
    // expensive query in the service. OutputCache also collapses concurrent
    // misses onto one execution, so a rollout cannot stampede the database.
    // Admin writes evict this by tag (ResponseCacheInvalidationMiddleware),
    // so cached does not mean stale.
    [OutputCache(Duration = 300, Tags = ["duas"])]
    [HttpGet("offline-sync")]
    public async Task<IActionResult> GetOfflineSync([FromQuery] DateTime? since)
    {
        var serverTime = DateTime.UtcNow;
        var result = await service.GetOfflineSyncAsync(since);
        Response.Headers["X-Sync-Server-Time"] = serverTime.ToString("O");
        return Ok(result);
    }

    [HttpGet("offline-ids")]
    [OutputCache(Duration = 60, Tags = ["duas"])]
    public async Task<IActionResult> GetOfflineIds()
        => Ok(await service.GetOfflineIdsAsync());

    [HttpGet("categories")]
    [OutputCache(Duration = 300, Tags = ["duas", "categories"])]
    public async Task<IActionResult> GetCategories(
        [FromQuery] bool published = true, [FromQuery] string? search = null,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null)
        => Ok(await service.GetCategoriesAsync(published, search, page, pageSize));

    [HttpGet("{id:guid}")]
    [OutputCache(Duration = 120, Tags = ["duas"])]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpGet("{id:guid}/admin")]
    public async Task<IActionResult> GetByIdForAdmin(Guid id)
    {
        var result = await service.GetByIdAsync(id, includeUnpublished: true);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveDuaRequest req)
    {
        var result = await service.CreateAsync(req);
        return Created($"/api/dua/{result.Id}", result);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveDuaRequest req)
    {
        var result = await service.UpdateAsync(id, req);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpPatch("{id:guid}/offline-availability")]
    public async Task<IActionResult> SetOfflineAvailability(Guid id, [FromBody] SetOfflineAvailabilityRequest req)
    {
        var result = await service.SetOfflineAvailabilityAsync(id, req.IsOfflineAvailable);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await service.DeleteAsync(id) ? NoContent() : NotFound();
}
