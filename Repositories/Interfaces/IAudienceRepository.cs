using Kidamooz.Domain.Entities;

namespace Kidamooz.Repositories.Interfaces;

public interface IAudienceRepository
{
    Task<List<AudienceSegment>> GetSegmentsAsync(CancellationToken ct = default);
    Task<List<AppUser>> GetUsersAsync(string? search, CancellationToken ct = default);
}
