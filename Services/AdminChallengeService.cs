using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Services;

public interface IAdminChallengeService
{
    Task<List<WeeklyChallengeAdminDto>> ListAsync(CancellationToken ct = default);
    Task<WeeklyChallengeAdminDto> CreateAsync(UpsertWeeklyChallengeRequestDto request, CancellationToken ct = default);
    Task<WeeklyChallengeAdminDto> UpdateAsync(Guid id, UpsertWeeklyChallengeRequestDto request, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class AdminChallengeService(AppDbContext db) : IAdminChallengeService
{
    public async Task<List<WeeklyChallengeAdminDto>> ListAsync(CancellationToken ct = default)
    {
        var items = await db.WeeklyChallenges.AsNoTracking()
            .OrderByDescending(x => x.WeekStart)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    public async Task<WeeklyChallengeAdminDto> CreateAsync(
        UpsertWeeklyChallengeRequestDto request,
        CancellationToken ct = default)
    {
        var (title, tag, description, weekStart, weekEnd, isActive) = Normalize(request);

        if (isActive)
            await DeactivateOverlappingAsync(weekStart, weekEnd, null, ct);

        var entity = new WeeklyChallenge
        {
            Id = Guid.NewGuid(),
            TitleFa = title,
            ThemeTag = tag,
            DescriptionFa = description,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.WeeklyChallenges.Add(entity);
        await db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<WeeklyChallengeAdminDto> UpdateAsync(
        Guid id,
        UpsertWeeklyChallengeRequestDto request,
        CancellationToken ct = default)
    {
        var entity = await db.WeeklyChallenges.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("چالش یافت نشد");

        var (title, tag, description, weekStart, weekEnd, isActive) = Normalize(request);

        if (isActive)
            await DeactivateOverlappingAsync(weekStart, weekEnd, id, ct);

        entity.TitleFa = title;
        entity.ThemeTag = tag;
        entity.DescriptionFa = description;
        entity.WeekStart = weekStart;
        entity.WeekEnd = weekEnd;
        entity.IsActive = isActive;

        await db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var entity = await db.WeeklyChallenges.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("چالش یافت نشد");

        if (isActive)
            await DeactivateOverlappingAsync(entity.WeekStart, entity.WeekEnd, id, ct);

        entity.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.WeeklyChallenges.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("چالش یافت نشد");

        db.WeeklyChallenges.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private async Task DeactivateOverlappingAsync(
        DateOnly weekStart,
        DateOnly weekEnd,
        Guid? exceptId,
        CancellationToken ct)
    {
        var overlapping = await db.WeeklyChallenges
            .Where(x => x.IsActive && x.WeekStart <= weekEnd && x.WeekEnd >= weekStart)
            .Where(x => exceptId == null || x.Id != exceptId)
            .ToListAsync(ct);

        foreach (var item in overlapping)
            item.IsActive = false;
    }

    private static (
        string Title,
        string Tag,
        string Description,
        DateOnly WeekStart,
        DateOnly WeekEnd,
        bool IsActive) Normalize(UpsertWeeklyChallengeRequestDto request)
    {
        var title = PlainTextSanitizer.Clean(request.TitleFa, 200);
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("عنوان الزامی است");

        var tag = PlainTextSanitizer.Clean(request.ThemeTag, 64);
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("تگ تم الزامی است");

        var description = PlainTextSanitizer.Clean(request.DescriptionFa, 1000);
        var weekStart = request.WeekStart;
        var weekEnd = request.WeekEnd ?? weekStart.AddDays(6);
        if (weekEnd < weekStart)
            throw new ArgumentException("پایان هفته نمی‌تواند قبل از شروع باشد");

        return (title, tag, description, weekStart, weekEnd, request.IsActive);
    }

    private static WeeklyChallengeAdminDto ToDto(WeeklyChallenge c) => new(
        c.Id,
        c.TitleFa,
        c.ThemeTag,
        c.DescriptionFa,
        c.WeekStart,
        c.WeekEnd,
        c.IsActive,
        c.CreatedAt);
}
