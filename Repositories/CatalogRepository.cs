using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Repositories;

public class CatalogRepository(AppDbContext db) : ICatalogRepository
{
    public async Task<CatalogMeta> GetMetaAsync(CancellationToken ct = default)
    {
        var meta = await db.CatalogMeta.FirstOrDefaultAsync(m => m.Id == 1, ct);
        if (meta != null)
            return meta;

        meta = new CatalogMeta
        {
            Id = 1,
            Version = $"{DateTimeOffset.UtcNow:O}-0-0",
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.CatalogMeta.Add(meta);
        await db.SaveChangesAsync(ct);
        return meta;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
