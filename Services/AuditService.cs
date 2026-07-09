using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.Mapping;
using Kidamooz.Repositories.Interfaces;

namespace Kidamooz.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string entityId, string entityTitle, string? details = null, CancellationToken ct = default);
    Task<List<AuditLogDto>> GetAsync(string? entityType, int limit, CancellationToken ct = default);
}

public class AuditService(IAuditLogRepository repository, ICurrentUserService currentUser) : IAuditService
{
    public async Task LogAsync(string action, string entityType, string entityId, string entityTitle, string? details = null, CancellationToken ct = default)
    {
        await repository.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityTitle = entityTitle,
            ActorEmail = currentUser.Email ?? "system",
            Details = details,
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);
    }

    public async Task<List<AuditLogDto>> GetAsync(string? entityType, int limit, CancellationToken ct = default)
    {
        var logs = await repository.GetAsync(entityType, limit, ct);
        return logs.Select(EntityMappers.ToAuditLogDto).ToList();
    }
}
