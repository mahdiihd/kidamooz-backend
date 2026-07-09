using Kidamooz.DTOs;
using Kidamooz.Repositories.Interfaces;
using Kidamooz.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Public;

[ApiController]
[Route("api/v1/catalog")]
public class PublicCatalogController(IPublicService publicService) : ControllerBase
{
    [HttpGet("version")]
    public async Task<ActionResult<CatalogVersionDto>> GetVersion(CancellationToken ct) =>
        Ok(await publicService.GetCatalogVersionAsync(ct));
}

[ApiController]
[Route("api/v1/categories")]
public class PublicCategoriesController(IPublicService publicService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken ct) =>
        Ok(await publicService.GetCategoriesAsync(ct));
}

[ApiController]
[Route("api/v1/stories")]
public class PublicStoriesController(IPublicService publicService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PublicStoryListResponseDto>> GetAll(
        [FromQuery] string? categoryId,
        [FromQuery] int? ageMin,
        [FromQuery] int? ageMax,
        [FromQuery] bool? featured,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50,
        [FromQuery] string sortBy = "sortOrder",
        CancellationToken ct = default)
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        var query = new StoryQuery(categoryId, ageMin, ageMax, featured, Published: true,
            UserId: userId, PublicOnly: true, Page: page, Limit: limit, SortBy: sortBy);
        return Ok(await publicService.GetStoriesAsync(query, ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PublicStoryDetailDto>> GetById(string id, CancellationToken ct)
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        var story = await publicService.GetStoryByIdAsync(id, userId, ct);
        return story == null ? NotFound() : Ok(story);
    }
}
