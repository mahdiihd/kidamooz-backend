using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Storage;
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
    [RequestSizeLimit(30 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StoryDetailDto>> Create([FromForm] StorySaveForm form, CancellationToken ct)
    {
        try
        {
            var result = await storyService.CreateWithMediaAsync(form, CollectChapterImages(), ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (MediaStorageException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StoryDetailDto>> Update(string id, [FromForm] StorySaveForm form, CancellationToken ct)
    {
        try
        {
            return Ok(await storyService.UpdateWithMediaAsync(id, form, CollectChapterImages(), ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MediaStorageException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
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

    private Dictionary<int, IFormFile> CollectChapterImages()
    {
        var images = new Dictionary<int, IFormFile>();
        const string prefix = "chapterImage_";

        foreach (var file in Request.Form.Files)
        {
            if (!file.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(file.Name[prefix.Length..], out var index))
                images[index] = file;
        }

        return images;
    }
}
