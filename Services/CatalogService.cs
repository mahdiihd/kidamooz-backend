using Kidamooz.DTOs;
using Kidamooz.Mapping;
using Kidamooz.Repositories.Interfaces;

namespace Kidamooz.Services;

public interface ICatalogService
{
    Task<CatalogVersionDto> GetVersionAsync(CancellationToken ct = default);
    Task<CatalogVersionDto> RebuildVersionAsync(CancellationToken ct = default);
    Task BumpVersionAsync(CancellationToken ct = default);
}

public class CatalogService(
    ICatalogRepository catalogRepository,
    IStoryRepository storyRepository,
    ICategoryRepository categoryRepository,
    IAuditService auditService) : ICatalogService
{
    public async Task<CatalogVersionDto> GetVersionAsync(CancellationToken ct = default)
    {
        var meta = await catalogRepository.GetMetaAsync(ct);
        return EntityMappers.ToCatalogVersionDto(meta);
    }

    public async Task<CatalogVersionDto> RebuildVersionAsync(CancellationToken ct = default)
    {
        await BumpVersionAsync(ct);
        await auditService.LogAsync("rebuild_version", "catalog", "1", "Catalog", ct: ct);
        return await GetVersionAsync(ct);
    }

    public async Task BumpVersionAsync(CancellationToken ct = default)
    {
        var meta = await catalogRepository.GetMetaAsync(ct);
        var publishedStories = await storyRepository.CountAsync(published: true, ct: ct);
        var publishedCategories = await categoryRepository.CountAsync(published: true, ct: ct);
        var now = DateTimeOffset.UtcNow;

        meta.Version = $"{now:O}-{publishedStories}-{publishedCategories}";
        meta.UpdatedAt = now;
        await catalogRepository.SaveChangesAsync(ct);
    }
}
