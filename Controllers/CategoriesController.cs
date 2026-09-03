using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryService service) : ControllerBase
{
    // Unpaged full tree — the filter dropdowns across the admin depend on this shape.
    [HttpGet]
    [OutputCache(Duration = 900, Tags = ["categories"])]
    public async Task<IActionResult> GetAll()
        => Ok(await service.GetAllAsync());

    // Paged top-level categories for the admin list screen.
    [HttpGet("paged")]
    [OutputCache(Duration = 300, Tags = ["categories"])]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] string? sort = null)
        => Ok(await service.GetPagedAsync(page, pageSize, search, sort));

    [HttpGet("{id:guid}")]
    [OutputCache(Duration = 300, Tags = ["categories"])]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    // How much content is attached, per module. Not cached: it backs the confirmation shown
    // before a delete or merge, where a stale count would be actively misleading.
    [HttpGet("{id:guid}/usage")]
    public async Task<IActionResult> GetUsage(Guid id)
        => Ok(await service.GetUsageAsync(id));

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
    {
        var result = await service.CreateAsync(req);
        return result.Status == CategoryWriteStatus.Ok
            ? CreatedAtAction(nameof(GetById), new { id = result.Category!.Id }, result.Category)
            : Problem(result);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest req)
    {
        var result = await service.UpdateAsync(id, req);
        return result.Status == CategoryWriteStatus.Ok ? Ok(result.Category) : Problem(result);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await service.DeleteAsync(id) ? NoContent() : NotFound();

    /// <summary>
    /// Moves all content and module memberships onto the target, then deletes this category.
    /// Without this the only way to consolidate two categories is to rename one, which moves no
    /// content and is how the duplicate titles were created.
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/merge")]
    public async Task<IActionResult> Merge(Guid id, [FromBody] MergeCategoryRequest req)
    {
        var result = await service.MergeAsync(id, req.TargetId);
        return result.Status == CategoryWriteStatus.Ok ? Ok(result.Category) : Problem(result);
    }

    // Route is matched before "{id:guid}" because "reorder" is not a Guid.
    [Authorize]
    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderCategoriesRequest req)
    {
        var result = await service.ReorderAsync(req);
        return result.Status == CategoryWriteStatus.Ok ? Ok(new { reordered = req.CategoryIds.Count }) : Problem(result);
    }

    IActionResult Problem(CategoryWriteResult result) => result.Status switch
    {
        CategoryWriteStatus.NotFound => NotFound(),
        CategoryWriteStatus.DuplicateTitle => Conflict(new { message = result.Message }),
        _ => BadRequest(new { message = result.Message }),
    };
}
