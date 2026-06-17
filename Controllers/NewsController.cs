using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/news")]
public class NewsController(INewsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? published = null)
        => Ok(await service.GetListAsync(page, pageSize, search, published));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveNewsRequest req)
    {
        var result = await service.CreateAsync(req);
        return Created($"/api/news/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveNewsRequest req)
    {
        var result = await service.UpdateAsync(id, req);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await service.DeleteAsync(id) ? NoContent() : NotFound();
}
