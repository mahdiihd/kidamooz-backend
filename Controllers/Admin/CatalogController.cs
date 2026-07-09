using Kidamooz.DTOs;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/catalog")]
[Authorize]
public class CatalogController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet("version")]
    public async Task<ActionResult<CatalogVersionDto>> GetVersion(CancellationToken ct) =>
        Ok(await catalogService.GetVersionAsync(ct));

    [HttpPost("rebuild-version")]
    public async Task<ActionResult<CatalogVersionDto>> RebuildVersion(CancellationToken ct) =>
        Ok(await catalogService.RebuildVersionAsync(ct));
}
