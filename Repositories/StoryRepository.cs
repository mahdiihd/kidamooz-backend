using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Repositories;

public class StoryRepository(AppDbContext db) : IStoryRepository
{
    public async Task<(List<Story> Items, int Total)> QueryAsync(StoryQuery query, CancellationToken ct = default)
    {
        var q = db.Stories
            .Include(s => s.AudienceSegments)
            .Include(s => s.AudienceUsers)
            .AsQueryable();

        if (!query.PublicOnly)
            q = q.Where(s => s.DeletedAt == null);
        else
            q = q.Where(s => s.DeletedAt == null && s.Published);

        if (query.CategoryId != null)
            q = q.Where(s => s.CategoryId == query.CategoryId);

        if (query.AgeMin.HasValue)
            q = q.Where(s => s.AgeMax >= query.AgeMin.Value);

        if (query.AgeMax.HasValue)
            q = q.Where(s => s.AgeMin <= query.AgeMax.Value);

        if (query.Featured.HasValue)
            q = q.Where(s => s.Featured == query.Featured.Value);

        if (query.Published.HasValue)
            q = q.Where(s => s.Published == query.Published.Value);

        if (query.Visibility != null)
            q = q.Where(s => s.Visibility == query.Visibility);

        if (query.PublicOnly)
        {
            if (string.IsNullOrEmpty(query.UserId))
                q = q.Where(s => s.Visibility == "public");
            else
                q = q.Where(s =>
                    s.Visibility == "public" ||
                    (s.Visibility == "restricted" && s.AudienceUsers.Any(u => u.UserId == query.UserId)));
        }

        var total = await q.CountAsync(ct);

        q = query.SortBy switch
        {
            "title" => q.OrderBy(s => s.TitleFa),
            "publishedAt" => q.OrderByDescending(s => s.PublishedAt),
            _ => q.OrderBy(s => s.SortOrder)
        };

        var items = await q
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Story?> GetByIdAsync(string id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var query = db.Stories
            .Include(s => s.Chapters)
            .Include(s => s.AudienceSegments)
            .Include(s => s.AudienceUsers)
            .Where(s => s.Id == id);

        if (!includeDeleted)
            query = query.Where(s => s.DeletedAt == null);

        return query.FirstOrDefaultAsync(ct);
    }

    public Task<bool> ExistsAsync(string id, CancellationToken ct = default) =>
        db.Stories.AnyAsync(s => s.Id == id && s.DeletedAt == null, ct);

    public async Task AddAsync(Story story, CancellationToken ct = default)
    {
        db.Stories.Add(story);
        await db.SaveChangesAsync(ct);
    }

    public Task<int> CountAsync(bool? published = null, bool? featured = null, CancellationToken ct = default)
    {
        var query = db.Stories.Where(s => s.DeletedAt == null);
        if (published.HasValue)
            query = query.Where(s => s.Published == published.Value);
        if (featured.HasValue)
            query = query.Where(s => s.Featured == featured.Value);
        return query.CountAsync(ct);
    }

    public Task<DateTimeOffset?> GetLastPublishedAtAsync(CancellationToken ct = default) =>
        db.Stories.Where(s => s.Published && s.DeletedAt == null && s.PublishedAt != null)
            .MaxAsync(s => (DateTimeOffset?)s.PublishedAt, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
