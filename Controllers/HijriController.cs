using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace IslamiJindegiApi.Controllers;

[ApiController]
public class HijriController(IHijriService service) : ControllerBase
{
    [HttpGet("api/hijri/date")]
    public async Task<IActionResult> GetDate(
        [FromQuery(Name = "country-code")] string? countryCode = "BD",
        [FromQuery] string? date = null)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return BadRequest(new { error = "country-code is required" });

        var (result, error) = await service.GetDateAsync(countryCode, date);
        if (error == "date must be yyyy-MM-dd") return BadRequest(new { error });
        if (result is null) return Problem("Could not resolve Hijri date for the given input.");
        return Ok(result);
    }

    [HttpGet("api/hijri/month")]
    public async Task<IActionResult> GetMonth(
        [FromQuery(Name = "country-code")] string? countryCode = "BD",
        [FromQuery(Name = "hijri-year")] int? hijriYear = null,
        [FromQuery(Name = "hijri-month")] int? hijriMonth = null)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || hijriYear is null || hijriMonth is null)
            return BadRequest(new { error = "country-code, hijri-year, and hijri-month are required" });

        var (result, error) = await service.GetMonthAsync(countryCode, hijriYear.Value, hijriMonth.Value);
        if (error is not null) return BadRequest(new { error });
        return Ok(result);
    }

    [HttpGet("api/hijri/sightings")]
    public async Task<IActionResult> GetSightings(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? countryCode = null, [FromQuery] int? hijriYear = null)
        => Ok(await service.GetSightingsAsync(page, pageSize, countryCode, hijriYear));

    [HttpGet("api/hijri/sightings/{id:guid}")]
    public async Task<IActionResult> GetSightingById(Guid id)
    {
        var result = await service.GetSightingByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("api/hijri/sightings")]
    public async Task<IActionResult> CreateSighting([FromBody] CreateHijriSightingRequest req)
    {
        var (item, error) = await service.CreateSightingAsync(req);
        if (error == "hijriMonth must be 1–12") return BadRequest(new { error });
        if (error is not null) return Conflict(new { error });
        return Created($"/api/hijri/sightings/{item!.Id}", item);
    }

    [HttpPut("api/hijri/sightings/{id:guid}")]
    public async Task<IActionResult> UpdateSighting(Guid id, [FromBody] UpdateHijriSightingRequest req)
    {
        var (item, error) = await service.UpdateSightingAsync(id, req);
        if (error == "hijriMonth must be 1–12") return BadRequest(new { error });
        if (error is not null) return Conflict(new { error });
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpDelete("api/hijri/sightings/{id:guid}")]
    public async Task<IActionResult> DeleteSighting(Guid id)
        => await service.DeleteSightingAsync(id) ? NoContent() : NotFound();
}
