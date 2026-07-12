using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Repositories;

public class DeviceTokenRepository(AppDbContext db) : IDeviceTokenRepository
{
    public async Task UpsertAsync(
        string token,
        string platform,
        string? appVersion,
        string? userId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var existing = await db.DeviceTokens.FirstOrDefaultAsync(x => x.Token == token, ct);
        if (existing == null)
        {
            db.DeviceTokens.Add(new DeviceToken
            {
                Token = token,
                Platform = platform,
                AppVersion = appVersion,
                UserId = userId,
                IsActive = true,
                CreatedAt = now,
                LastSeenAt = now,
            });
        }
        else
        {
            existing.Platform = platform;
            existing.AppVersion = appVersion;
            existing.UserId = userId ?? existing.UserId;
            existing.IsActive = true;
            existing.LastSeenAt = now;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(string token, CancellationToken ct = default)
    {
        var existing = await db.DeviceTokens.FirstOrDefaultAsync(x => x.Token == token, ct);
        if (existing == null)
        {
            return;
        }

        existing.IsActive = false;
        existing.LastSeenAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeactivateManyAsync(IEnumerable<string> tokens, CancellationToken ct = default)
    {
        var tokenList = tokens.Distinct().ToList();
        if (tokenList.Count == 0)
        {
            return;
        }

        var rows = await db.DeviceTokens
            .Where(x => tokenList.Contains(x.Token))
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var row in rows)
        {
            row.IsActive = false;
            row.LastSeenAt = now;
        }

        if (rows.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<DeviceToken>> GetActiveAsync(
        string platform,
        IReadOnlyCollection<string>? userIds = null,
        CancellationToken ct = default)
    {
        var query = db.DeviceTokens.Where(x => x.IsActive && x.Platform == platform);
        if (userIds is { Count: > 0 })
        {
            query = query.Where(x => x.UserId != null && userIds.Contains(x.UserId));
        }

        return await query.ToListAsync(ct);
    }
}
