using Kidamooz.Domain.Entities;

namespace Kidamooz.Repositories.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<List<AuditLog>> GetAsync(string? entityType, int limit, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
