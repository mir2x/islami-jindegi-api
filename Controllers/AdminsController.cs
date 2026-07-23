using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/admins")]
[Authorize]
public class AdminsController(IAdminService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList() => Ok(await service.GetListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminRequest req)
        => Ok(await service.CreateAsync(req));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await service.DeleteAsync(id) ? NoContent() : NotFound();
}
