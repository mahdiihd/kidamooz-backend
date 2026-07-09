using Kidamooz.DTOs;
using Kidamooz.Repositories.Interfaces;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/stories")]
[Authorize]
public class StoriesController(IStoryService storyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StoryListResponseDto>> GetAll(
        [FromQuery] string? categoryId,
        [FromQuery] int? ageMin,
        [FromQuery] int? ageMax,
        [FromQuery] bool? featured,
        [FromQuery] bool? published,
        [FromQuery] string? visibility,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50,
        [FromQuery] string sortBy = "sortOrder",
        CancellationToken ct = default)
    {
        var query = new StoryQuery(categoryId, ageMin, ageMax, featured, published, visibility,
            Page: page, Limit: limit, SortBy: sortBy);
        return Ok(await storyService.GetAllAsync(query, ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StoryDetailDto>> GetById(string id, CancellationToken ct)
    {
        try
        {
            return Ok(await storyService.GetByIdAsync(id, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<StoryDetailDto>> Create([FromBody] StoryPayloadDto payload, CancellationToken ct)
    {
        try
        {
            var result = await storyService.CreateAsync(payload, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<StoryDetailDto>> Update(string id, [FromBody] StoryPayloadDto payload, CancellationToken ct)
    {
        try
        {
            return Ok(await storyService.UpdateAsync(id, payload, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        try
        {
            await storyService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/publish")]
    public async Task<ActionResult<StoryDto>> Publish(string id, [FromBody] PublishRequestDto request, CancellationToken ct)
    {
        try
        {
            return Ok(await storyService.PublishAsync(id, request.Published, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/featured")]
    public async Task<ActionResult<StoryDto>> Featured(string id, [FromBody] FeaturedRequestDto request, CancellationToken ct)
    {
        try
        {
            return Ok(await storyService.ToggleFeaturedAsync(id, request.Featured, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/chapters")]
    public async Task<ActionResult<StoryDetailDto>> UpdateChapters(string id, [FromBody] ChaptersRequestDto request, CancellationToken ct)
    {
        try
        {
            return Ok(await storyService.UpdateChaptersAsync(id, request.Chapters, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("reorder")]
    public async Task<ActionResult<List<StoryDto>>> Reorder([FromBody] ReorderRequestDto request, CancellationToken ct) =>
        Ok(await storyService.ReorderAsync(request.Ids, ct));
}
