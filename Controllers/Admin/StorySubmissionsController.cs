using Kidamooz.DTOs;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/v1/admin/story-submissions")]
public class StorySubmissionsController(IStoryDraftService storyDraftService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<StoryDraftDto>>> List(
        [FromQuery] string? status,
        CancellationToken ct)
    {
        if (string.Equals(status, "pending_review", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(await storyDraftService.AdminListPendingAsync(ct));
        }

        return Ok(await storyDraftService.AdminListAsync(status, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StoryDraftDto>> Get(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await storyDraftService.AdminGetAsync(id, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApproveStoryDraftResponseDto>> Approve(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await storyDraftService.AdminApproveAsync(id, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<StoryDraftDto>> Reject(
        Guid id,
        [FromBody] RejectStoryDraftRequestDto? request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await storyDraftService.AdminRejectAsync(id, request?.Reason, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}
