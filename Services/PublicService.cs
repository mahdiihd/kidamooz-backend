using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Storage;
using Kidamooz.Mapping;
using Kidamooz.Repositories.Interfaces;

namespace Kidamooz.Services;

public interface IPublicService
{
    Task<CatalogVersionDto> GetCatalogVersionAsync(CancellationToken ct = default);
    Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<PublicStoryListResponseDto> GetStoriesAsync(StoryQuery query, CancellationToken ct = default);
    Task<PublicStoryDetailDto?> GetStoryByIdAsync(string id, string? userId, CancellationToken ct = default);
}

public class PublicService(
    ICatalogService catalogService,
    ICategoryRepository categoryRepository,
    IStoryRepository storyRepository,
    IMediaUrlNormalizer mediaUrls) : IPublicService
{
    public Task<CatalogVersionDto> GetCatalogVersionAsync(CancellationToken ct = default) =>
        catalogService.GetVersionAsync(ct);

    public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await categoryRepository.GetPublishedAsync(ct);
        return categories.Select(c => mediaUrls.Normalize(EntityMappers.ToCategoryDto(c))).ToList();
    }

    public async Task<PublicStoryListResponseDto> GetStoriesAsync(StoryQuery query, CancellationToken ct = default)
    {
        var publicQuery = query with { PublicOnly = true, Published = true };
        var (items, total) = await storyRepository.QueryAsync(publicQuery, ct);
        return new PublicStoryListResponseDto(
            items.Select(s => mediaUrls.Normalize(EntityMappers.ToPublicStoryDto(s))).ToList(),
            total);
    }

    public async Task<PublicStoryDetailDto?> GetStoryByIdAsync(string id, string? userId, CancellationToken ct = default)
    {
        var story = await storyRepository.GetByIdAsync(id, ct: ct);
        if (story == null || !story.Published)
            return null;

        if (story.Visibility == "restricted")
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            var hasAccess = story.AudienceUsers.Any(u => u.UserId == userId);
            if (!hasAccess)
                return null;
        }

        return mediaUrls.Normalize(EntityMappers.ToPublicStoryDetailDto(story));
    }
}
