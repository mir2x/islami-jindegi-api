using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/timezone")]
public class TimezoneController(ITimezoneService service) : ControllerBase
{
    // A coordinate's time zone changes only when a border moves, so this is
    // cached for a day. Clients cache the answer against the coordinate that
    // produced it and only call again when they actually move.
    [HttpGet]
    [OutputCache(Duration = 86400, VaryByQueryKeys = ["lat", "lng"], Tags = ["timezone"])]
    public IActionResult Get([FromQuery] double? lat = null, [FromQuery] double? lng = null)
    {
        if (lat is null || lng is null)
            return BadRequest(new { error = "lat and lng are required" });

        var (result, error) = service.Resolve(lat.Value, lng.Value);
        if (error is not null) return BadRequest(new { error });
        return Ok(result);
    }
}
