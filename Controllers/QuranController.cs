using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace IslamiJindegiApi.Controllers;

public record MushafUrlReq(string MushafId);
public record TafsirUrlReq(string TafsirId);
public record DbUrlReq(string DbName);
public record SuraAudioUrlsReq(string ReciterId, int Sura);

[ApiController]
[Route("api/quran")]
public class QuranController(IQuranService service) : ControllerBase
{
    [HttpGet("surahs")]
    public IActionResult GetSurahs()
        => Ok(service.GetSurahs());

    [HttpGet("surahs/{number:int}/ayahs")]
    public async Task<IActionResult> GetSurahAyahs(int number, [FromQuery] string? translator)
    {
        var result = await service.GetSurahAyahsAsync(number, translator);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("mushafs")]
    public IActionResult GetMushafs()
        => Ok(service.GetMushafs());

    [HttpGet("mushafs/{editionId}")]
    public IActionResult GetMushaf(string editionId)
    {
        var edition = service.GetMushaf(editionId);
        return edition is null
            ? NotFound(new { error = $"Unknown edition: {editionId}" })
            : Ok(edition);
    }

    [HttpPost("mushaf-url")]
    public async Task<IActionResult> GetMushafUrl([FromBody] MushafUrlReq req)
    {
        if (string.IsNullOrWhiteSpace(req.MushafId) || !service.IsValidMushaf(req.MushafId))
            return BadRequest(new { error = "Invalid or missing mushafId." });

        var (url, sizeTask) = service.GetMushafUrl(req.MushafId);
        var size = await sizeTask;
        return Ok(new { url, sizeBytes = size });
    }

    [HttpPost("tafsir-url")]
    public async Task<IActionResult> GetTafsirUrl([FromBody] TafsirUrlReq req)
    {
        if (string.IsNullOrWhiteSpace(req.TafsirId) || !service.IsValidTafsir(req.TafsirId))
            return BadRequest(new { error = "Invalid or missing tafsirId." });

        var (url, sizeTask) = service.GetTafsirUrl(req.TafsirId);
        var size = await sizeTask;
        return Ok(new { url, sizeBytes = size });
    }

    [HttpPost("db-url")]
    public async Task<IActionResult> GetDbUrl([FromBody] DbUrlReq req)
    {
        if (string.IsNullOrWhiteSpace(req.DbName) || !service.IsValidDb(req.DbName))
            return BadRequest(new { error = "Invalid or missing dbName." });

        var (url, sizeTask) = service.GetDbUrl(req.DbName);
        var size = await sizeTask;
        return Ok(new { url, sizeBytes = size });
    }

    [HttpPost("sura-audio-urls")]
    public async Task<IActionResult> GetSuraAudioUrls([FromBody] SuraAudioUrlsReq req)
    {
        if (string.IsNullOrWhiteSpace(req.ReciterId))
            return BadRequest(new { error = "Missing required parameter: reciterId." });
        if (req.Sura < 1 || req.Sura > 114)
            return BadRequest(new { error = "Invalid sura number. Must be 1–114." });

        var result = await service.GetSuraAudioUrlsAsync(req.ReciterId, req.Sura);
        return Ok(result);
    }
}
