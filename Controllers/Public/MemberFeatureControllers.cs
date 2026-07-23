using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Public;

[ApiController]
[Authorize(Roles = "member")]
[Route("api/v1/me/children")]
public class MemberChildrenController(
    IChildProfileService children,
    IMemberContext member) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ChildProfileDto>>> List(CancellationToken ct) =>
        Ok(await children.ListAsync(RequireUserId(), ct));

    [HttpPost]
    public async Task<ActionResult<ChildProfileDto>> Create(
        [FromBody] UpsertChildProfileRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var created = await children.CreateAsync(RequireUserId(), request, ct);
            return CreatedAtAction(nameof(List), created);
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

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ChildProfileDto>> Update(
        Guid id,
        [FromBody] UpsertChildProfileRequestDto request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await children.UpdateAsync(RequireUserId(), id, request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await children.DeleteAsync(RequireUserId(), id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    private string RequireUserId() =>
        member.UserId ?? throw new UnauthorizedAccessException("ورود الزامی است.");
}

[ApiController]
[Authorize(Roles = "member")]
[Route("api/v1/me/favorites")]
public class MemberFavoritesController(
    IMemberFavoriteService favorites,
    IMemberContext member) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SyncFavoritesResponseDto>> List(CancellationToken ct) =>
        Ok(await favorites.ListAsync(RequireUserId(), ct));

    [HttpPut]
    public async Task<ActionResult<SyncFavoritesResponseDto>> Sync(
        [FromBody] SyncFavoritesRequestDto request,
        CancellationToken ct) =>
        Ok(await favorites.SyncAsync(RequireUserId(), request, ct));

    [HttpPost("{storyId}/toggle")]
    public async Task<ActionResult<object>> Toggle(string storyId, CancellationToken ct)
    {
        try
        {
            var added = await favorites.ToggleAsync(RequireUserId(), storyId, ct);
            return Ok(new { isFavorite = added });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private string RequireUserId() =>
        member.UserId ?? throw new UnauthorizedAccessException("ورود الزامی است.");
}

[ApiController]
[Route("api/v1/engagement")]
public class PublicEngagementController(IMemberEngagementService engagement) : ControllerBase
{
    [HttpGet("story-of-the-day")]
    public async Task<ActionResult<StoryOfTheDayDto>> StoryOfTheDay(CancellationToken ct)
    {
        var item = await engagement.GetStoryOfTheDayAsync(ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("weekly-challenge")]
    public async Task<ActionResult<WeeklyChallengeDto>> WeeklyChallenge(CancellationToken ct)
    {
        var item = await engagement.GetActiveChallengeAsync(ct);
        return item is null ? NotFound() : Ok(item);
    }
}

[ApiController]
[Authorize(Roles = "member")]
[Route("api/v1/me/engagement")]
public class MemberEngagementController(
    IMemberEngagementService engagement,
    IMemberContext member) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MemberEngagementDto>> Get(CancellationToken ct) =>
        Ok(await engagement.GetEngagementAsync(RequireUserId(), ct));

    [HttpPost("listen")]
    public async Task<ActionResult<MemberEngagementDto>> RecordListen(
        [FromBody] RecordListenRequestDto request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await engagement.RecordListenAsync(RequireUserId(), request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("redeem-plus")]
    public async Task<ActionResult<MemberEngagementDto>> RedeemPlus(
        [FromBody] RedeemPlusRequestDto request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await engagement.RedeemPlusAsync(RequireUserId(), request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string RequireUserId() =>
        member.UserId ?? throw new UnauthorizedAccessException("ورود الزامی است.");
}
