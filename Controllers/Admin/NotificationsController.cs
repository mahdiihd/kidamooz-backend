using Kidamooz.DTOs;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpPost("broadcast")]
    public async Task<ActionResult<BroadcastNotificationResponseDto>> Broadcast(
        [FromBody] BroadcastNotificationRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var result = await notificationService.BroadcastAsync(request, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }
}
