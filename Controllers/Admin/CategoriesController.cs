using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Storage;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/categories")]
[Authorize]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken ct) =>
        Ok(await categoryService.GetAllAsync(ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetById(string id, CancellationToken ct)
    {
        try
        {
            return Ok(await categoryService.GetByIdAsync(id, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CategoryDto>> Create([FromForm] CategorySaveForm form, CancellationToken ct)
    {
        try
        {
            var result = await categoryService.CreateWithMediaAsync(form, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (MediaStorageException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = ex.Message,
                detail = ex.GetType().Name
            });
        }
    }

    [HttpPut("{id}")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CategoryDto>> Update(string id, [FromForm] CategorySaveForm form, CancellationToken ct)
    {
        try
        {
            return Ok(await categoryService.UpdateWithMediaAsync(id, form, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MediaStorageException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = ex.Message,
                detail = ex.GetType().Name
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        try
        {
            await categoryService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/publish")]
    public async Task<ActionResult<CategoryDto>> Publish(string id, [FromBody] PublishRequestDto request, CancellationToken ct)
    {
        try
        {
            return Ok(await categoryService.PublishAsync(id, request.Published, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("reorder")]
    public async Task<ActionResult<List<CategoryDto>>> Reorder([FromBody] ReorderRequestDto request, CancellationToken ct) =>
        Ok(await categoryService.ReorderAsync(request.Ids, ct));
}
