using Kidamooz.DTOs;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/story-of-the-day")]
[Authorize]
public class AdminStoryOfTheDayController(IAdminStoryOfTheDayService storyOfTheDay) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminStoryOfTheDayDto?>> GetToday(CancellationToken ct) =>
        Ok(await storyOfTheDay.GetTodayAsync(ct));

    [HttpPut]
    public async Task<ActionResult<AdminStoryOfTheDayDto>> SetToday(
        [FromBody] SetStoryOfTheDayRequestDto request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await storyOfTheDay.SetTodayAsync(request.StoryId, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> ClearToday(
        [FromQuery] string? storyId,
        CancellationToken ct)
    {
        await storyOfTheDay.ClearTodayAsync(storyId, ct);
        return NoContent();
    }
}
