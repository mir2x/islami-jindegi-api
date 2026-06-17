using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController(IArticleService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null, [FromQuery] Guid? authorId = null,
        [FromQuery] Guid? categoryId = null, [FromQuery] bool? published = null)
        => Ok(await service.GetListAsync(page, pageSize, search, authorId, categoryId, published));

    [HttpGet("authors")]
    public async Task<IActionResult> GetAuthors([FromQuery] bool published = true)
        => Ok(await service.GetAuthorsAsync(published));

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] bool published = true)
        => Ok(await service.GetCategoriesAsync(published));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveArticleRequest req)
    {
        var result = await service.CreateAsync(req);
        return Created($"/api/articles/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveArticleRequest req)
    {
        var result = await service.UpdateAsync(id, req);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await service.DeleteAsync(id) ? NoContent() : NotFound();
}
