using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController(IAuthorService service) : ControllerBase
{
    [HttpGet]
    [OutputCache(Duration = 120, Tags = ["authors"])]
    public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] string? sort = null)
        => Ok(await service.GetListAsync(page, pageSize, search, sort));

    // Unpaged full list — the admin pickers, merge target dropdown and reorder screen need every
    // author. Route is matched before "{id:guid}" because "all" is not a Guid.
    [HttpGet("all")]
    [OutputCache(Duration = 900, Tags = ["authors"])]
    public async Task<IActionResult> GetAll()
        => Ok(await service.GetAllAsync());

    [HttpGet("{id:guid}")]
    [OutputCache(Duration = 120, Tags = ["authors"])]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    // How much content is attributed, per module. Not cached: it backs the confirmation shown
    // before a delete or merge, where a stale count would be actively misleading.
    [HttpGet("{id:guid}/usage")]
    public async Task<IActionResult> GetUsage(Guid id)
        => Ok(await service.GetUsageAsync(id));

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAuthorRequest req)
    {
        var result = await service.CreateAsync(req);
        return result.Status == AuthorWriteStatus.Ok
            ? CreatedAtAction(nameof(GetById), new { id = result.Author!.Id }, result.Author)
            : Problem(result);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAuthorRequest req)
    {
        var result = await service.UpdateAsync(id, req);
        return result.Status == AuthorWriteStatus.Ok ? Ok(result.Author) : Problem(result);
    }

    // Refused while the author still owns content — see AuthorService.DeleteAsync.
    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);
        return result.Status == AuthorWriteStatus.Ok ? NoContent() : Problem(result);
    }

    /// <summary>
    /// Moves all content and module memberships onto the target, then deletes this author.
    /// Without this the only way to consolidate two authors is to rename one, which moves no
    /// content and is how the duplicate names were created.
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/merge")]
    public async Task<IActionResult> Merge(Guid id, [FromBody] MergeAuthorRequest req)
    {
        var result = await service.MergeAsync(id, req.TargetId);
        return result.Status == AuthorWriteStatus.Ok ? Ok(result.Author) : Problem(result);
    }

    // Route is matched before "{id:guid}" because "reorder" is not a Guid.
    [Authorize]
    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderAuthorsRequest req)
    {
        var result = await service.ReorderAsync(req);
        return result.Status == AuthorWriteStatus.Ok ? Ok(new { reordered = req.AuthorIds.Count }) : Problem(result);
    }

    IActionResult Problem(AuthorWriteResult result) => result.Status switch
    {
        AuthorWriteStatus.NotFound => NotFound(),
        AuthorWriteStatus.DuplicateName => Conflict(new { message = result.Message }),
        AuthorWriteStatus.HasContent => Conflict(new { message = result.Message }),
        _ => BadRequest(new { message = result.Message }),
    };
}
