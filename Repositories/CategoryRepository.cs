using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Repositories;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public Task<List<Category>> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default)
    {
        var query = db.Categories.AsQueryable();
        if (!includeDeleted)
            query = query.Where(c => c.DeletedAt == null);
        return query.OrderBy(c => c.SortOrder).ToListAsync(ct);
    }

    public Task<List<Category>> GetPublishedAsync(CancellationToken ct = default) =>
        db.Categories.Where(c => c.Published && c.DeletedAt == null)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

    public Task<Category?> GetByIdAsync(string id, CancellationToken ct = default) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, ct);

    public Task<Category?> FindDeletedByIdOrSlugAsync(string id, string slug, CancellationToken ct = default) =>
        db.Categories.FirstOrDefaultAsync(
            c => c.DeletedAt != null && (c.Id == id || c.Slug == slug),
            ct);

    public Task<bool> ExistsAsync(string id, CancellationToken ct = default) =>
        db.Categories.AnyAsync(c => c.Id == id && c.DeletedAt == null, ct);

    public Task<bool> SlugExistsAsync(string slug, string? excludeId = null, CancellationToken ct = default)
    {
        var query = db.Categories.Where(c => c.Slug == slug && c.DeletedAt == null);
        if (excludeId != null)
            query = query.Where(c => c.Id != excludeId);
        return query.AnyAsync(ct);
    }

    public async Task AddAsync(Category category, CancellationToken ct = default)
    {
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
    }

    public Task<int> CountAsync(bool? published = null, CancellationToken ct = default)
    {
        var query = db.Categories.Where(c => c.DeletedAt == null);
        if (published.HasValue)
            query = query.Where(c => c.Published == published.Value);
        return query.CountAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
