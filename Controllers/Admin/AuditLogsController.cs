using Kidamooz.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kidamooz.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/audit-logs")]
[Authorize]
public class AuditLogsController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? entityType,
        [FromQuery] int limit = 50,
        CancellationToken ct = default) =>
        Ok(await auditService.GetAsync(entityType, limit, ct));
}
