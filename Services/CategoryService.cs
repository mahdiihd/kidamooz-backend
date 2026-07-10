using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Storage;
using Kidamooz.Mapping;
using Kidamooz.Repositories.Interfaces;

namespace Kidamooz.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CategoryPayloadDto payload, CancellationToken ct = default);
    Task<CategoryDto> CreateWithMediaAsync(CategorySaveForm form, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(string id, CategoryPayloadDto payload, CancellationToken ct = default);
    Task<CategoryDto> UpdateWithMediaAsync(string id, CategorySaveForm form, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<CategoryDto> PublishAsync(string id, bool published, CancellationToken ct = default);
    Task<List<CategoryDto>> ReorderAsync(List<string> ids, CancellationToken ct = default);
}

public class CategoryService(
    ICategoryRepository repository,
    ICatalogService catalogService,
    IAuditService auditService,
    IMediaService mediaService) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await repository.GetAllAsync(ct: ct);
        return items.Select(EntityMappers.ToCategoryDto).ToList();
    }

    public async Task<CategoryDto> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var category = await repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("دسته‌بندی یافت نشد");
        return EntityMappers.ToCategoryDto(category);
    }

    public async Task<CategoryDto> CreateWithMediaAsync(CategorySaveForm form, CancellationToken ct = default)
    {
        var iconUrl = await ResolveIconUrlAsync(form.Icon, form.IconUrl, required: true, ct);
        return await CreateAsync(form.ToPayload(iconUrl), ct);
    }

    public async Task<CategoryDto> UpdateWithMediaAsync(string id, CategorySaveForm form, CancellationToken ct = default)
    {
        var existing = await repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("دسته‌بندی یافت نشد");

        var iconUrl = await ResolveIconUrlAsync(
            form.Icon,
            string.IsNullOrWhiteSpace(form.IconUrl) ? existing.IconUrl : form.IconUrl,
            required: true,
            ct);

        return await UpdateAsync(id, form.ToPayload(iconUrl), ct);
    }

    public async Task<CategoryDto> CreateAsync(CategoryPayloadDto payload, CancellationToken ct = default)
    {
        var id = payload.Id ?? payload.Slug;
        if (await repository.ExistsAsync(id, ct))
            throw new InvalidOperationException("شناسه تکراری است");
        if (await repository.SlugExistsAsync(payload.Slug, ct: ct))
            throw new InvalidOperationException("اسلاگ تکراری است");

        var now = DateTimeOffset.UtcNow;
        var category = new Domain.Entities.Category
        {
            Id = id,
            Slug = payload.Slug,
            TitleFa = payload.Title.Fa,
            TitleEn = payload.Title.En,
            IconUrl = payload.IconUrl,
            Color = payload.Color,
            SortOrder = payload.SortOrder,
            Published = payload.Published,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(category, ct);
        await auditService.LogAsync("create", "category", category.Id, category.TitleFa, ct: ct);
        return EntityMappers.ToCategoryDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(string id, CategoryPayloadDto payload, CancellationToken ct = default)
    {
        var category = await repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("دسته‌بندی یافت نشد");

        if (await repository.SlugExistsAsync(payload.Slug, excludeId: id, ct: ct))
            throw new InvalidOperationException("اسلاگ تکراری است");

        var wasPublished = category.Published;
        category.Slug = payload.Slug;
        category.TitleFa = payload.Title.Fa;
        category.TitleEn = payload.Title.En;
        category.IconUrl = payload.IconUrl;
        category.Color = payload.Color;
        category.SortOrder = payload.SortOrder;
        category.Published = payload.Published;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.SaveChangesAsync(ct);
        await auditService.LogAsync("update", "category", category.Id, category.TitleFa, ct: ct);

        if (wasPublished || category.Published)
            await catalogService.BumpVersionAsync(ct);

        return EntityMappers.ToCategoryDto(category);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var category = await repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("دسته‌بندی یافت نشد");

        var wasPublished = category.Published;
        category.DeletedAt = DateTimeOffset.UtcNow;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);
        await auditService.LogAsync("delete", "category", category.Id, category.TitleFa, ct: ct);

        if (wasPublished)
            await catalogService.BumpVersionAsync(ct);
    }

    public async Task<CategoryDto> PublishAsync(string id, bool published, CancellationToken ct = default)
    {
        var category = await repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("دسته‌بندی یافت نشد");

        category.Published = published;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(ct);
        await auditService.LogAsync(published ? "publish" : "unpublish", "category", category.Id, category.TitleFa, ct: ct);
        await catalogService.BumpVersionAsync(ct);
        return EntityMappers.ToCategoryDto(category);
    }

    public async Task<List<CategoryDto>> ReorderAsync(List<string> ids, CancellationToken ct = default)
    {
        var categories = await repository.GetAllAsync(ct: ct);
        var map = categories.ToDictionary(c => c.Id);

        for (var i = 0; i < ids.Count; i++)
        {
            if (map.TryGetValue(ids[i], out var category))
            {
                category.SortOrder = i + 1;
                category.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await repository.SaveChangesAsync(ct);
        await auditService.LogAsync("reorder", "category", "all", "Categories", ct: ct);
        await catalogService.BumpVersionAsync(ct);

        return (await repository.GetAllAsync(ct: ct)).Select(EntityMappers.ToCategoryDto).ToList();
    }

    private async Task<string> ResolveIconUrlAsync(
        IFormFile? icon,
        string? existingUrl,
        bool required,
        CancellationToken ct)
    {
        if (icon is { Length: > 0 })
        {
            try
            {
                var result = await mediaService.UploadAsync(icon, "icon", ct);
                return result.Url;
            }
            catch (ArgumentException ex)
            {
                throw new MediaStorageException(ex.Message, ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new MediaStorageException(ex.Message, ex);
            }
            catch (MediaStorageException)
            {
                throw;
            }
        }

        if (!string.IsNullOrWhiteSpace(existingUrl) && IsRemoteUrl(existingUrl))
            return existingUrl;

        if (required)
            throw new MediaStorageException("آیکون الزامی است");

        return existingUrl ?? string.Empty;
    }

    private static bool IsRemoteUrl(string url) =>
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
}
