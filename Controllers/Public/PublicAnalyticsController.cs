using Kidamooz.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Public;

[ApiController]
[Route("api/v1/analytics")]
public class PublicAnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpPost("app-open")]
    public async Task<IActionResult> RecordAppOpen(CancellationToken ct)
    {
        await analyticsService.RecordAppOpenAsync(ct);
        return NoContent();
    }
}
