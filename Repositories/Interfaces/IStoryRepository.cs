using Kidamooz.Domain.Entities;

namespace Kidamooz.Repositories.Interfaces;

public record StoryQuery(
    string? CategoryId = null,
    int? AgeMin = null,
    int? AgeMax = null,
    bool? Featured = null,
    bool? Published = null,
    string? Visibility = null,
    string? UserId = null,
    bool PublicOnly = false,
    int Page = 1,
    int Limit = 50,
    string SortBy = "sortOrder");

public interface IStoryRepository
{
    Task<(List<Story> Items, int Total)> QueryAsync(StoryQuery query, CancellationToken ct = default);
    Task<Story?> GetByIdAsync(string id, bool includeDeleted = false, CancellationToken ct = default);
    Task<bool> ExistsAsync(string id, CancellationToken ct = default);
    Task AddAsync(Story story, CancellationToken ct = default);
    Task<int> CountAsync(bool? published = null, bool? featured = null, CancellationToken ct = default);
    Task<DateTimeOffset?> GetLastPublishedAtAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
