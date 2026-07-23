using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.Infrastructure.Storage;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Public;

[ApiController]
[Authorize(Roles = "member")]
[Route("api/v1/me/story-drafts")]
public class StoryDraftsController(
    IStoryDraftService storyDraftService,
    IMemberContext member) : ControllerBase
{
    public const string DeviceIdHeader = "X-Device-Id";

    [HttpGet]
    public async Task<ActionResult<List<StoryDraftDto>>> List(CancellationToken ct)
    {
        try
        {
            return Ok(await storyDraftService.ListAsync(RequireUserId(), ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("quota")]
    public async Task<ActionResult<StoryDraftQuotaDto>> Quota(CancellationToken ct)
    {
        try
        {
            return Ok(await storyDraftService.GetQuotaAsync(RequireUserId(), ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StoryDraftDto>> Get(Guid id, CancellationToken ct)
    {
        return await Execute(() => storyDraftService.GetAsync(RequireUserId(), id, ct));
    }

    [HttpPost]
    [RequestSizeLimit(15 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 15 * 1024 * 1024)]
    public async Task<ActionResult<StoryDraftDto>> Create([FromForm] IFormFile drawing, CancellationToken ct)
    {
        try
        {
            var result = await storyDraftService.CreateFromDrawingAsync(
                RequireUserId(),
                DeviceId(),
                drawing,
                ct);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        catch (DailyStoryLimitException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                code = "daily_limit_reached",
                message = "هر روز فقط یک‌بار می‌توانی نقاشی بارگذاری کنی و با هوش مصنوعی قصه بسازی.",
                nextAvailableAt = ex.NextAvailableAt
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (MediaStorageException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<StoryDraftDto>> Update(
        Guid id,
        [FromBody] UpdateStoryDraftRequestDto request,
        CancellationToken ct)
    {
        return await Execute(() => storyDraftService.UpdateAsync(RequireUserId(), id, request, ct));
    }

    [HttpPost("{id:guid}/rewrite")]
    public async Task<ActionResult<StoryDraftDto>> Rewrite(
        Guid id,
        [FromBody] RewriteStoryDraftRequestDto? request,
        CancellationToken ct)
    {
        return await Execute(() =>
            storyDraftService.RewriteAsync(RequireUserId(), id, request ?? new RewriteStoryDraftRequestDto(null), ct));
    }

    [HttpPost("{id:guid}/cover/regenerate")]
    public async Task<ActionResult<StoryDraftDto>> RegenerateCover(
        Guid id,
        [FromBody] RegenerateCoverRequestDto? request,
        CancellationToken ct)
    {
        return await Execute(() => storyDraftService.RegenerateCoverAsync(RequireUserId(), id, request, ct));
    }

    [HttpPost("{id:guid}/audio")]
    [RequestSizeLimit(40 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 40 * 1024 * 1024)]
    public async Task<ActionResult<StoryDraftDto>> UploadAudio(
        Guid id,
        [FromForm] IFormFile audio,
        [FromForm] int? durationSeconds,
        CancellationToken ct)
    {
        try
        {
            return Ok(await storyDraftService.UploadAudioAsync(RequireUserId(), id, audio, durationSeconds, ct));
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
        catch (MediaStorageException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<StoryDraftDto>> Submit(Guid id, CancellationToken ct)
    {
        return await Execute(() => storyDraftService.SubmitForReviewAsync(RequireUserId(), id, ct));
    }

    private async Task<ActionResult<StoryDraftDto>> Execute(Func<Task<StoryDraftDto>> action)
    {
        try
        {
            return Ok(await action());
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
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    private string RequireUserId() =>
        member.UserId ?? throw new UnauthorizedAccessException("ورود الزامی است.");

    private string DeviceId()
    {
        if (!Request.Headers.TryGetValue(DeviceIdHeader, out var values))
            return string.Empty;
        return values.ToString();
    }
}
