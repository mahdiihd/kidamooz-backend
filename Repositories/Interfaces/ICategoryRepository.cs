using Kidamooz.Domain.Entities;

namespace Kidamooz.Repositories.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default);
    Task<List<Category>> GetPublishedAsync(CancellationToken ct = default);
    Task<Category?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Category?> FindDeletedByIdOrSlugAsync(string id, string slug, CancellationToken ct = default);
    Task<bool> ExistsAsync(string id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, string? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    Task<int> CountAsync(bool? published = null, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
