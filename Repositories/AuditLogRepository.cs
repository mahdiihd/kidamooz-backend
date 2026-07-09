using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Repositories;

public class AuditLogRepository(AppDbContext db) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog log, CancellationToken ct = default)
    {
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<AuditLog>> GetAsync(string? entityType, int limit, CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsQueryable();
        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(l => l.EntityType == entityType);
        return query.OrderByDescending(l => l.CreatedAt).Take(limit).ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
