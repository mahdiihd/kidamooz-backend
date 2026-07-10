using Kidamooz.DTOs;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "admin")]
public class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AdminUserDto>>> GetAll(CancellationToken ct) =>
        Ok(await adminUserService.GetAllAsync(ct));

    [HttpPost]
    public async Task<ActionResult<AdminUserDto>> Create([FromBody] CreateAdminUserRequestDto request, CancellationToken ct)
    {
        try
        {
            var user = await adminUserService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetAll), user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetAdminPasswordRequestDto request, CancellationToken ct)
    {
        try
        {
            await adminUserService.ResetPasswordAsync(id, request, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await adminUserService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
