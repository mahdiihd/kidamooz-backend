using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Services;

public interface IMemberFavoriteService
{
    Task<SyncFavoritesResponseDto> ListAsync(string userId, CancellationToken ct = default);
    Task<SyncFavoritesResponseDto> SyncAsync(string userId, SyncFavoritesRequestDto request, CancellationToken ct = default);
    Task<bool> ToggleAsync(string userId, string storyId, CancellationToken ct = default);
}

public class MemberFavoriteService(AppDbContext db) : IMemberFavoriteService
{
    public async Task<SyncFavoritesResponseDto> ListAsync(string userId, CancellationToken ct = default)
    {
        EnsureUser(userId);
        var ids = await db.MemberFavorites.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.StoryId)
            .ToListAsync(ct);
        return new SyncFavoritesResponseDto(ids);
    }

    public async Task<SyncFavoritesResponseDto> SyncAsync(
        string userId,
        SyncFavoritesRequestDto request,
        CancellationToken ct = default)
    {
        EnsureUser(userId);
        var incoming = (request.StoryIds ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .Take(200)
            .ToList();

        var existing = await db.MemberFavorites.Where(x => x.UserId == userId).ToListAsync(ct);
        var existingIds = existing.Select(x => x.StoryId).ToHashSet(StringComparer.Ordinal);

        var toRemove = existing.Where(x => !incoming.Contains(x.StoryId, StringComparer.Ordinal)).ToList();
        if (toRemove.Count > 0)
            db.MemberFavorites.RemoveRange(toRemove);

        var publishedIds = await db.Stories.AsNoTracking()
            .Where(x => incoming.Contains(x.Id) && x.Published && x.DeletedAt == null)
            .Select(x => x.Id)
            .ToListAsync(ct);
        var publishedSet = publishedIds.ToHashSet(StringComparer.Ordinal);

        var now = DateTimeOffset.UtcNow;
        foreach (var storyId in incoming.Where(id => publishedSet.Contains(id) && !existingIds.Contains(id)))
        {
            db.MemberFavorites.Add(new MemberFavorite
            {
                UserId = userId,
                StoryId = storyId,
                CreatedAt = now
            });
        }

        await db.SaveChangesAsync(ct);
        return await ListAsync(userId, ct);
    }

    public async Task<bool> ToggleAsync(string userId, string storyId, CancellationToken ct = default)
    {
        EnsureUser(userId);
        if (string.IsNullOrWhiteSpace(storyId))
            throw new ArgumentException("شناسه قصه الزامی است.");

        var id = storyId.Trim();
        var existing = await db.MemberFavorites.FirstOrDefaultAsync(
            x => x.UserId == userId && x.StoryId == id, ct);
        if (existing is not null)
        {
            db.MemberFavorites.Remove(existing);
            await db.SaveChangesAsync(ct);
            return false;
        }

        var storyExists = await db.Stories.AsNoTracking()
            .AnyAsync(x => x.Id == id && x.Published && x.DeletedAt == null, ct);
        if (!storyExists)
            throw new KeyNotFoundException("قصه یافت نشد.");

        db.MemberFavorites.Add(new MemberFavorite
        {
            UserId = userId,
            StoryId = id,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static void EnsureUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("ورود الزامی است.");
    }
}
