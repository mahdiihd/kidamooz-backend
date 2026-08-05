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

    [HttpPost("{id}/credit")]
    public async Task<ActionResult<MemberWalletDto>> GrantCredit(
        string id,
        [FromBody] AdminGrantCreditRequestDto request,
        [FromServices] IWalletService wallet,
        CancellationToken ct)
    {
        try
        {
            return Ok(await wallet.AdminGrantAsync(id, request.AmountTomans, request.Note, ct));
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
}
