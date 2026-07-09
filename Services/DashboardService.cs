using Kidamooz.DTOs;
using Kidamooz.Mapping;
using Kidamooz.Repositories.Interfaces;

namespace Kidamooz.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default);
}

public class DashboardService(
    IStoryRepository storyRepository,
    ICategoryRepository categoryRepository,
    IDashboardRepository dashboardRepository,
    ICatalogService catalogService) : IDashboardService
{
    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var totalStories = await storyRepository.CountAsync(ct: ct);
        var publishedStories = await storyRepository.CountAsync(published: true, ct: ct);
        var featuredStories = await storyRepository.CountAsync(featured: true, ct: ct);
        var totalCategories = await categoryRepository.CountAsync(ct: ct);
        var publishedCategories = await categoryRepository.CountAsync(published: true, ct: ct);
        var views = await dashboardRepository.GetViewStatsAsync(ct);
        var catalogVersion = await catalogService.GetVersionAsync(ct);
        var lastPublishedAt = await storyRepository.GetLastPublishedAtAsync(ct);

        return new DashboardStatsDto(
            totalStories,
            publishedStories,
            totalStories - publishedStories,
            featuredStories,
            totalCategories,
            publishedCategories,
            new DashboardViewsDto(views.Total, views.Today, views.ThisWeek),
            catalogVersion,
            lastPublishedAt);
    }
}
