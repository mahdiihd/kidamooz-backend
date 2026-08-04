using Kidamooz.DTOs;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/challenges")]
[Authorize]
public class AdminChallengesController(IAdminChallengeService challenges) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<WeeklyChallengeAdminDto>>> List(CancellationToken ct) =>
        Ok(await challenges.ListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<WeeklyChallengeAdminDto>> Create(
        [FromBody] UpsertWeeklyChallengeRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var created = await challenges.CreateAsync(request, ct);
            return CreatedAtAction(nameof(List), created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WeeklyChallengeAdminDto>> Update(
        Guid id,
        [FromBody] UpsertWeeklyChallengeRequestDto request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await challenges.UpdateAsync(id, request, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(
        Guid id,
        [FromBody] SetChallengeActiveRequestDto request,
        CancellationToken ct)
    {
        try
        {
            await challenges.SetActiveAsync(id, request.IsActive, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await challenges.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
