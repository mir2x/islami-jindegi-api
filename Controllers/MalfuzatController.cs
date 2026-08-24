using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/malfuzat")]
public class MalfuzatController(IMalfuzatService service, PopupAuthorResolver popupAuthor) : ControllerBase
{
    [HttpGet]
    [OutputCache(Duration = 120, Tags = ["malfuzats", "authors", "categories"])]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null, [FromQuery] Guid? authorId = null,
        [FromQuery] Guid? categoryId = null, [FromQuery] bool? published = null,
        [FromQuery] bool? hasAudio = null, [FromQuery] bool? offlineAvailable = null,
        [FromQuery] string? sort = null,
        [FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null)
        => Ok(await service.GetListAsync(page, pageSize, search, await ResolveLegacyAuthorAsync(authorId), categoryId, published, hasAudio, offlineAvailable, sort, dateFrom, dateTo));

    /// The malfuzat author id the legacy Ruby backend used for Mufti Mansurul
    /// Haq. App builds from March 2025 onward hardcoded it into the home-screen
    /// popup, and the .NET migration reissued the author's key — so every
    /// installed copy has been asking for an author that no longer exists and
    /// silently getting zero results.
    ///
    /// Forwarding it to the live id restores those installs without a store
    /// release. Current app builds use `GET /malfuzat/daily` and send no author
    /// key at all, so this shim only ever serves old clients and can be deleted
    /// once they have aged out.
    static readonly Guid LegacyPopupAuthorId = Guid.Parse("6842ab90-27d0-4ef9-b783-3b03388a2304");

    async Task<Guid?> ResolveLegacyAuthorAsync(Guid? authorId)
        => authorId == LegacyPopupAuthorId
            ? await popupAuthor.ResolveAsync() ?? authorId
            : authorId;

    /// One random published, text-only malfuzat by the popup author.
    ///
    /// Deliberately not output-cached: the response is meant to be random per
    /// request, and each device asks at most once a day.
    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily()
    {
        var result = await service.GetDailyPopupAsync();
        return result is null ? NoContent() : Ok(result);
    }

    // Cached because every client that has no watermark yet asks for the
    // same full-corpus response. Without this, each device re-runs the most
    // expensive query in the service. OutputCache also collapses concurrent
    // misses onto one execution, so a rollout cannot stampede the database.
    // Admin writes evict this by tag (ResponseCacheInvalidationMiddleware),
    // so cached does not mean stale.
    [OutputCache(Duration = 300, Tags = ["malfuzats"])]
    [HttpGet("offline-sync")]
    public async Task<IActionResult> GetOfflineSync([FromQuery] DateTime? since)
    {
        var serverTime = DateTime.UtcNow;
        var result = await service.GetOfflineSyncAsync(since);
        Response.Headers["X-Sync-Server-Time"] = serverTime.ToString("O");
        return Ok(result);
    }

    [HttpGet("offline-ids")]
    [OutputCache(Duration = 60, Tags = ["malfuzats"])]
    public async Task<IActionResult> GetOfflineIds()
        => Ok(await service.GetOfflineIdsAsync());

    [HttpGet("authors")]
    [OutputCache(Duration = 300, Tags = ["malfuzats", "authors"])]
    public async Task<IActionResult> GetAuthors(
        [FromQuery] bool published = true, [FromQuery] string? search = null,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null)
        => Ok(await service.GetAuthorsAsync(published, search, page, pageSize));

    [HttpGet("categories")]
    [OutputCache(Duration = 300, Tags = ["malfuzats", "categories"])]
    public async Task<IActionResult> GetCategories(
        [FromQuery] bool published = true, [FromQuery] string? search = null,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null)
        => Ok(await service.GetCategoriesAsync(published, search, page, pageSize));

    [HttpGet("{id:guid}")]
    [OutputCache(Duration = 120, Tags = ["malfuzats"])]
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
    public async Task<IActionResult> Create([FromBody] SaveMalfuzatRequest req)
    {
        var (item, error) = await service.CreateAsync(req);
        if (error is not null) return BadRequest(error);
        return Created($"/api/malfuzat/{item!.Id}", item);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveMalfuzatRequest req)
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
