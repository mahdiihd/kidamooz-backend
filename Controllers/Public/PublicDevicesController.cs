using Kidamooz.DTOs;
using Kidamooz.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Public;

[ApiController]
[Route("api/v1/devices")]
public class PublicDevicesController(IDeviceService deviceService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] DeviceRegisterRequestDto request, CancellationToken ct)
    {
        try
        {
            await deviceService.RegisterAsync(request, ct);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("unregister")]
    public async Task<IActionResult> Unregister([FromBody] DeviceUnregisterRequestDto request, CancellationToken ct)
    {
        try
        {
            await deviceService.UnregisterAsync(request, ct);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
