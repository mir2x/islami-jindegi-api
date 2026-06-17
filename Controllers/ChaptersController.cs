using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace IslamiJindegiApi.Controllers;

[ApiController]
public class ChaptersController(IChapterService service) : ControllerBase
{
    [HttpGet("api/chapters")]
    public async Task<IActionResult> GetChapters(
        [FromQuery] Guid? bookId, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await service.GetChaptersAsync(page, pageSize, bookId, search));

    [HttpGet("api/subchapters")]
    public async Task<IActionResult> GetSubChapters(
        [FromQuery] Guid? bookId, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await service.GetSubChaptersAsync(page, pageSize, bookId, search));

    [HttpGet("api/books/{bookId:guid}/chapters")]
    public async Task<IActionResult> GetChaptersByBook(Guid bookId)
        => Ok(await service.GetChaptersByBookAsync(bookId));

    [HttpGet("api/chapters/{id:guid}")]
    public async Task<IActionResult> GetChapter(Guid id)
    {
        var result = await service.GetChapterByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("api/subchapters/{id:guid}")]
    public async Task<IActionResult> GetSubChapter(Guid id)
    {
        var result = await service.GetSubChapterByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("api/books/{bookId:guid}/chapters")]
    public async Task<IActionResult> CreateChapter(Guid bookId, [FromBody] SaveChapterRequest req)
    {
        var (chapter, bookNotFound) = await service.CreateChapterAsync(bookId, req);
        if (bookNotFound) return NotFound();
        return Created($"/api/chapters/{chapter!.Id}", chapter);
    }

    [HttpPut("api/chapters/{id:guid}")]
    public async Task<IActionResult> UpdateChapter(Guid id, [FromBody] SaveChapterRequest req)
    {
        var result = await service.UpdateChapterAsync(id, req);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("api/chapters/{id:guid}")]
    public async Task<IActionResult> DeleteChapter(Guid id)
        => await service.DeleteChapterAsync(id) ? NoContent() : NotFound();

    [HttpPost("api/subchapters")]
    public async Task<IActionResult> CreateSubChapter([FromBody] CreateSubChapterRequest req)
    {
        var (sub, chapterNotFound) = await service.CreateSubChapterAsync(req);
        if (chapterNotFound) return NotFound();
        return Created($"/api/subchapters/{sub!.Id}", sub);
    }

    [HttpPost("api/chapters/{chapterId:guid}/subchapters")]
    public async Task<IActionResult> CreateSubChapterUnderChapter(Guid chapterId, [FromBody] SaveSubChapterRequest req)
    {
        var (sub, chapterNotFound) = await service.CreateSubChapterUnderChapterAsync(chapterId, req);
        if (chapterNotFound) return NotFound();
        return Created($"/api/subchapters/{sub!.Id}", sub);
    }

    [HttpPut("api/subchapters/{id:guid}")]
    public async Task<IActionResult> UpdateSubChapter(Guid id, [FromBody] SaveSubChapterRequest req)
    {
        var result = await service.UpdateSubChapterAsync(id, req);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("api/subchapters/{id:guid}")]
    public async Task<IActionResult> DeleteSubChapter(Guid id)
        => await service.DeleteSubChapterAsync(id) ? NoContent() : NotFound();
}
