using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AppSettingsResponse>> Get()
    {
        var setting = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync();
        return Ok(setting is null
            ? new AppSettingsResponse(AskQuestion: true, DisplayOfflineQuran: false)
            : new AppSettingsResponse(setting.AskQuestion, setting.DisplayOfflineQuran));
    }

    [Authorize]
    [HttpPut]
    public async Task<ActionResult<AppSettingsResponse>> Update(
        [FromBody] UpdateAppSettingsRequest request)
    {
        var setting = await db.AppSettings.FirstOrDefaultAsync();
        if (setting is null)
        {
            setting = new AppSetting { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
            db.AppSettings.Add(setting);
        }

        setting.AskQuestion = request.AskQuestion;
        setting.DisplayOfflineQuran = request.DisplayOfflineQuran;
        setting.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new AppSettingsResponse(setting.AskQuestion, setting.DisplayOfflineQuran));
    }
}
