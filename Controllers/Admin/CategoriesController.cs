using Kidamooz.DTOs;
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
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryPayloadDto payload, CancellationToken ct)
    {
        try
        {
            return CreatedAtAction(nameof(GetById), new { id = payload.Id ?? payload.Slug },
                await categoryService.CreateAsync(payload, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryDto>> Update(string id, [FromBody] CategoryPayloadDto payload, CancellationToken ct)
    {
        try
        {
            return Ok(await categoryService.UpdateAsync(id, payload, ct));
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
