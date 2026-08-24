using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/masail")]
public class MasailController(IMasailService service) : ControllerBase
{
    [HttpGet]
    [OutputCache(Duration = 120, Tags = ["masails", "authors", "categories"])]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null, [FromQuery] Guid? authorId = null,
        [FromQuery] Guid? categoryId = null, [FromQuery] bool? published = null,
        [FromQuery] bool? hasAudio = null, [FromQuery] bool? offlineAvailable = null,
        [FromQuery] string? sort = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null)
        => Ok(await service.GetListAsync(page, pageSize, search, authorId, categoryId, published, hasAudio, offlineAvailable, sort, dateFrom, dateTo));

    // Cached because every client that has no watermark yet asks for the
    // same full-corpus response. Without this, each device re-runs the most
    // expensive query in the service. OutputCache also collapses concurrent
    // misses onto one execution, so a rollout cannot stampede the database.
    // Admin writes evict this by tag (ResponseCacheInvalidationMiddleware),
    // so cached does not mean stale.
    [OutputCache(Duration = 300, Tags = ["masails"])]
    [HttpGet("offline-sync")]
    public async Task<IActionResult> GetOfflineSync([FromQuery] DateTime? since)
    {
        var serverTime = DateTime.UtcNow;
        var result = await service.GetOfflineSyncAsync(since);
        Response.Headers["X-Sync-Server-Time"] = serverTime.ToString("O");
        return Ok(result);
    }

    [HttpGet("offline-ids")]
    [OutputCache(Duration = 60, Tags = ["masails"])]
    public async Task<IActionResult> GetOfflineIds()
        => Ok(await service.GetOfflineIdsAsync());

    [HttpGet("authors")]
    [OutputCache(Duration = 300, Tags = ["masails", "authors"])]
    public async Task<IActionResult> GetAuthors(
        [FromQuery] bool published = true, [FromQuery] string? search = null,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null)
        => Ok(await service.GetAuthorsAsync(published, search, page, pageSize));

    [HttpGet("categories")]
    [OutputCache(Duration = 300, Tags = ["masails", "categories"])]
    public async Task<IActionResult> GetCategories(
        [FromQuery] bool published = true, [FromQuery] string? search = null,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null)
        => Ok(await service.GetCategoriesAsync(published, search, page, pageSize));

    [HttpGet("{id:guid}")]
    [OutputCache(Duration = 120, Tags = ["masails"])]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] string? scope = null)
    {
        var result = await service.GetByIdAsync(id, hasAudio: ScopeToHasAudio(scope));
        return result is null ? NotFound() : Ok(result);
    }

    // The Text/Audio tabs are a permanent partition of the corpus, so
    // previous/next are scoped to them. Anything else (a share link, a
    // bookmark, the All tab) leaves the sequence corpus-wide.
    static bool? ScopeToHasAudio(string? scope) => scope switch
    {
        "audio" => true,
        "text" => false,
        _ => null,
    };

    [Authorize]
    [HttpGet("{id:guid}/admin")]
    public async Task<IActionResult> GetByIdForAdmin(Guid id)
    {
        var result = await service.GetByIdAsync(id, includeUnpublished: true);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveMasailRequest req)
    {
        var result = await service.CreateAsync(req);
        return Created($"/api/masail/{result.Id}", result);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveMasailRequest req)
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
