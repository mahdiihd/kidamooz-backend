using Kidamooz.DTOs;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokensDto>> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        try
        {
            return Ok(await authService.LoginAsync(request, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokensDto>> Refresh([FromBody] RefreshRequestDto request, CancellationToken ct)
    {
        try
        {
            return Ok(await authService.RefreshAsync(request, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request, CancellationToken ct)
    {
        await authService.LogoutAsync(request, ct);
        return NoContent();
    }
}
