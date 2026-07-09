using Kidamooz.DTOs;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/audience")]
[Authorize]
public class AudienceController(IAudienceService audienceService) : ControllerBase
{
    [HttpGet("segments")]
    public async Task<ActionResult<List<AudienceSegmentDto>>> GetSegments(CancellationToken ct) =>
        Ok(await audienceService.GetSegmentsAsync(ct));

    [HttpGet("users")]
    public async Task<ActionResult<List<AudienceUserDto>>> GetUsers([FromQuery] string? q, CancellationToken ct) =>
        Ok(await audienceService.GetUsersAsync(q, ct));
}
