using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace IslamiJindegiApi.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(IAdminService adminService) : ControllerBase
{
    [HttpPost("google")]
    public async Task<IActionResult> GoogleSignIn([FromBody] GoogleSignInRequest req)
    {
        var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
            ?? throw new InvalidOperationException("GOOGLE_CLIENT_ID not set.");

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(req.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [clientId]
            });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized();
        }

        var admin = await adminService.GetByEmailAsync(payload.Email);
        if (admin is null) return StatusCode(StatusCodes.Status403Forbidden);

        var secret = Environment.GetEnvironmentVariable("ADMIN_JWT_SECRET")
            ?? throw new InvalidOperationException("ADMIN_JWT_SECRET not set.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.Email, admin.Email)],
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: creds);

        return Ok(new AuthResponse(new JwtSecurityTokenHandler().WriteToken(jwt), admin.Email));
    }
}
