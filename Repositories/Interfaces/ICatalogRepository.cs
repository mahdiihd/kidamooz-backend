using Kidamooz.Domain.Entities;

namespace Kidamooz.Repositories.Interfaces;

public interface ICatalogRepository
{
    Task<CatalogMeta> GetMetaAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
