using Kidamooz.DTOs;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/members")]
[Authorize]
public class AdminMembersController(IAdminMemberService adminMemberService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppMemberAdminDto>>> List(
        [FromQuery] string? q,
        CancellationToken ct) =>
        Ok(await adminMemberService.ListAsync(q, ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        try
        {
            await adminMemberService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
