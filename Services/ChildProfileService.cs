using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Services;

public interface IChildProfileService
{
    Task<List<ChildProfileDto>> ListAsync(string userId, CancellationToken ct = default);
    Task<ChildProfileDto> CreateAsync(string userId, UpsertChildProfileRequestDto request, CancellationToken ct = default);
    Task<ChildProfileDto> UpdateAsync(string userId, Guid id, UpsertChildProfileRequestDto request, CancellationToken ct = default);
    Task DeleteAsync(string userId, Guid id, CancellationToken ct = default);
}

public class ChildProfileService(AppDbContext db) : IChildProfileService
{
    private const int MaxChildren = 5;

    public async Task<List<ChildProfileDto>> ListAsync(string userId, CancellationToken ct = default)
    {
        EnsureUser(userId);
        var items = await db.ChildProfiles.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<ChildProfileDto> CreateAsync(
        string userId,
        UpsertChildProfileRequestDto request,
        CancellationToken ct = default)
    {
        EnsureUser(userId);
        var count = await db.ChildProfiles.CountAsync(x => x.UserId == userId, ct);
        if (count >= MaxChildren)
            throw new InvalidOperationException("حداکثر پنج پروفایل کودک می‌توانی بسازی.");

        var now = DateTimeOffset.UtcNow;
        var child = new ChildProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = RequireName(request.Name),
            Age = ClampAge(request.Age),
            AvatarKey = NormalizeAvatar(request.AvatarKey),
            IsActive = count == 0,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.ChildProfiles.Add(child);
        await db.SaveChangesAsync(ct);
        return ToDto(child);
    }

    public async Task<ChildProfileDto> UpdateAsync(
        string userId,
        Guid id,
        UpsertChildProfileRequestDto request,
        CancellationToken ct = default)
    {
        var child = await GetOwnedAsync(userId, id, ct);
        child.Name = RequireName(request.Name);
        child.Age = ClampAge(request.Age);
        child.AvatarKey = NormalizeAvatar(request.AvatarKey);
        child.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(child);
    }

    public async Task DeleteAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var child = await GetOwnedAsync(userId, id, ct);
        db.ChildProfiles.Remove(child);
        await db.SaveChangesAsync(ct);
    }

    private async Task<ChildProfile> GetOwnedAsync(string userId, Guid id, CancellationToken ct)
    {
        EnsureUser(userId);
        var child = await db.ChildProfiles.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("پروفایل کودک یافت نشد.");
        if (!string.Equals(child.UserId, userId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("دسترسی مجاز نیست.");
        return child;
    }

    private static void EnsureUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("ورود الزامی است.");
    }

    private static string RequireName(string name)
    {
        var cleaned = PlainTextSanitizer.Clean(name, 100);
        if (string.IsNullOrWhiteSpace(cleaned))
            throw new ArgumentException("نام کودک الزامی است.");
        return cleaned;
    }

    private static int ClampAge(int age) => Math.Clamp(age, 1, 14);

    private static string NormalizeAvatar(string? key)
    {
        var cleaned = PlainTextSanitizer.Clean(key ?? "moon", 32).ToLowerInvariant();
        return string.IsNullOrWhiteSpace(cleaned) ? "moon" : cleaned;
    }

    private static ChildProfileDto ToDto(ChildProfile c) => new(
        c.Id,
        c.Name,
        c.Age,
        c.AvatarKey,
        c.IsActive,
        c.CreatedAt,
        c.UpdatedAt);
}
