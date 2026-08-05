using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Public;

[ApiController]
[Authorize(Roles = "member")]
[Route("api/v1/me/wallet")]
public class MemberWalletController(
    IWalletService wallet,
    IMemberContext member) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MemberWalletDto>> Get(CancellationToken ct) =>
        Ok(await wallet.GetWalletAsync(RequireUserId(), ct));

    [HttpPost("bazaar/confirm")]
    public async Task<ActionResult<MemberWalletDto>> ConfirmBazaar(
        [FromBody] ConfirmBazaarPurchaseRequestDto request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await wallet.ConfirmBazaarPurchaseAsync(RequireUserId(), request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    private string RequireUserId()
    {
        var userId = member.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException();
        return userId;
    }
}
