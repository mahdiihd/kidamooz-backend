using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Public;

[ApiController]
[Route("api/v1/auth")]
public class MemberAuthController(IMemberAuthService memberAuth) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login-or-register")]
    public async Task<ActionResult<MemberAuthResponseDto>> LoginOrRegister(
        [FromBody] MemberAuthRequestDto request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await memberAuth.LoginOrRegisterAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "member")]
    [HttpGet("me")]
    public async Task<ActionResult<MemberProfileDto>> Me(
        [FromServices] IMemberContext member,
        CancellationToken ct)
    {
        try
        {
            return Ok(await memberAuth.GetProfileAsync(member.UserId!, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "member")]
    [HttpPatch("me")]
    public async Task<ActionResult<MemberProfileDto>> UpdateMe(
        [FromServices] IMemberContext member,
        [FromBody] UpdateMemberProfileRequestDto request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await memberAuth.UpdateProfileAsync(member.UserId!, request, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
