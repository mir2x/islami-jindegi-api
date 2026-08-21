using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.OutputCaching;
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
    [OutputCache(Duration = 3600, Tags = ["quran"])]
    public IActionResult GetSurahs()
        => Ok(service.GetSurahs());

    // translations/tafsirs: omit = defaults (all translations, no tafsirs), "none" = excluded,
    // "all" = everything, or a comma-separated allow-list (translator names / tafsir ids).
    // `translator`/`tafsir` are kept as single-value aliases for backwards compatibility.
    [HttpGet("surahs/{number:int}/ayahs")]
    [OutputCache(Duration = 3600, Tags = ["quran"])]
    public async Task<IActionResult> GetSurahAyahs(
        int number,
        [FromQuery] string? translator, [FromQuery] string? translations,
        [FromQuery] string? tafsir, [FromQuery] string? tafsirs,
        [FromQuery] bool words = true)
    {
        var result = await service.GetSurahAyahsAsync(number, translations ?? translator, words, tafsirs ?? tafsir);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("surahs/{number:int}/ayahs/{ayahNumber:int}")]
    [OutputCache(Duration = 3600, Tags = ["quran"])]
    public async Task<IActionResult> GetSurahAyah(
        int number, int ayahNumber,
        [FromQuery] string? translator, [FromQuery] string? translations,
        [FromQuery] string? tafsir, [FromQuery] string? tafsirs,
        [FromQuery] bool words = true)
    {
        var result = await service.GetAyahAsync(number, ayahNumber, translations ?? translator, words, tafsirs ?? tafsir);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Missing required query parameter: q." });
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "Invalid page/pageSize." });

        var result = await service.SearchAsync(q, page, pageSize);
        return Ok(result);
    }

    [HttpGet("translators")]
    [OutputCache(Duration = 3600, Tags = ["quran"])]
    public async Task<IActionResult> GetTranslators()
        => Ok(await service.GetTranslatorsAsync());

    [HttpGet("tafsirs")]
    [OutputCache(Duration = 3600, Tags = ["quran"])]
    public IActionResult GetTafsirs()
        => Ok(service.GetTafsirs());

    [HttpGet("reciters")]
    [OutputCache(Duration = 3600, Tags = ["quran"])]
    public IActionResult GetReciters()
        => Ok(service.GetReciters());

    [HttpGet("mushafs")]
    [OutputCache(Duration = 3600, Tags = ["quran"])]
    public IActionResult GetMushafs()
        => Ok(service.GetMushafs());

    [HttpGet("mushafs/{editionId}")]
    [OutputCache(Duration = 3600, Tags = ["quran"])]
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
        if (string.IsNullOrWhiteSpace(req.ReciterId) || !service.IsValidReciter(req.ReciterId))
            return BadRequest(new { error = "Invalid or missing reciterId." });
        if (req.Sura < 1 || req.Sura > 114)
            return BadRequest(new { error = "Invalid sura number. Must be 1–114." });

        var result = await service.GetSuraAudioUrlsAsync(req.ReciterId, req.Sura);
        return Ok(result);
    }
}
