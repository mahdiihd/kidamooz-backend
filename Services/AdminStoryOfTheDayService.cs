using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Kidamooz.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Services;

public interface IAdminStoryOfTheDayService
{
    Task<AdminStoryOfTheDayDto?> GetTodayAsync(CancellationToken ct = default);
    Task<AdminStoryOfTheDayDto> SetTodayAsync(string storyId, CancellationToken ct = default);
    Task ClearTodayAsync(string? onlyIfStoryId, CancellationToken ct = default);
}

public class AdminStoryOfTheDayService(AppDbContext db) : IAdminStoryOfTheDayService
{
    public async Task<AdminStoryOfTheDayDto?> GetTodayAsync(CancellationToken ct = default)
    {
        var today = TehranTime.TodayTehran();
        var pick = await db.StoriesOfTheDay.AsNoTracking()
            .Include(x => x.Story)
            .FirstOrDefaultAsync(x => x.PickDate == today, ct);

        if (pick?.Story is null)
            return null;

        return new AdminStoryOfTheDayDto(
            pick.PickDate,
            pick.StoryId,
            pick.Story.TitleFa,
            pick.Story.Published && pick.Story.DeletedAt is null);
    }

    public async Task<AdminStoryOfTheDayDto> SetTodayAsync(string storyId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storyId))
            throw new ArgumentException("شناسه قصه الزامی است");

        var story = await db.Stories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == storyId && x.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException("قصه یافت نشد");

        if (!story.Published)
            throw new InvalidOperationException("فقط قصه منتشرشده می‌تواند قصه امروز باشد");

        var today = TehranTime.TodayTehran();
        var pick = await db.StoriesOfTheDay.FirstOrDefaultAsync(x => x.PickDate == today, ct);
        if (pick is null)
        {
            pick = new StoryOfTheDay
            {
                PickDate = today,
                StoryId = story.Id,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.StoriesOfTheDay.Add(pick);
        }
        else
        {
            pick.StoryId = story.Id;
        }

        await db.SaveChangesAsync(ct);

        return new AdminStoryOfTheDayDto(today, story.Id, story.TitleFa, true);
    }

    public async Task ClearTodayAsync(string? onlyIfStoryId, CancellationToken ct = default)
    {
        var today = TehranTime.TodayTehran();
        var pick = await db.StoriesOfTheDay.FirstOrDefaultAsync(x => x.PickDate == today, ct);
        if (pick is null)
            return;

        if (!string.IsNullOrWhiteSpace(onlyIfStoryId) &&
            !string.Equals(pick.StoryId, onlyIfStoryId, StringComparison.Ordinal))
            return;

        db.StoriesOfTheDay.Remove(pick);
        await db.SaveChangesAsync(ct);
    }
}
