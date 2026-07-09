using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Repositories;

public class AudienceRepository(AppDbContext db) : IAudienceRepository
{
    public Task<List<AudienceSegment>> GetSegmentsAsync(CancellationToken ct = default) =>
        db.AudienceSegments.Where(s => s.IsActive).OrderBy(s => s.Id).ToListAsync(ct);

    public Task<List<AppUser>> GetUsersAsync(string? search, CancellationToken ct = default)
    {
        var query = db.AppUsers.Where(u => u.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.DisplayName.Contains(term) ||
                u.Email.Contains(term) ||
                u.Id.Contains(term));
        }
        return query.OrderBy(u => u.DisplayName).ToListAsync(ct);
    }
}
