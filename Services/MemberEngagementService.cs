using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Kidamooz.Infrastructure;
using Kidamooz.Infrastructure.Security;
using Kidamooz.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Services;

public interface IMemberEngagementService
{
    Task<StoryOfTheDayDto?> GetStoryOfTheDayAsync(CancellationToken ct = default);
    Task<WeeklyChallengeDto?> GetActiveChallengeAsync(CancellationToken ct = default);
    Task<MemberEngagementDto> GetEngagementAsync(string userId, CancellationToken ct = default);
    Task<MemberEngagementDto> RecordListenAsync(string userId, RecordListenRequestDto request, CancellationToken ct = default);
    Task RecordCreateActivityAsync(string userId, CancellationToken ct = default);
    Task<MemberEngagementDto> RedeemPlusAsync(string userId, RedeemPlusRequestDto request, CancellationToken ct = default);
}

public class MemberEngagementService(
    AppDbContext db,
    IMediaUrlNormalizer mediaUrls) : IMemberEngagementService
{
    private static readonly HashSet<string> PlusCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "KIDINGO-PLUS",
        "KIDAMOOZ-PLUS"
    };

    public async Task<StoryOfTheDayDto?> GetStoryOfTheDayAsync(CancellationToken ct = default)
    {
        var today = TehranTime.TodayTehran();
        var pick = await db.StoriesOfTheDay.AsNoTracking()
            .Include(x => x.Story)
            .FirstOrDefaultAsync(x => x.PickDate == today, ct);

        if (pick?.Story is { Published: true, DeletedAt: null })
            return ToStoryOfDay(pick.PickDate, pick.Story);

        var featured = await db.Stories.AsNoTracking()
            .Where(x => x.Published && x.DeletedAt == null && x.Featured)
            .OrderBy(x => x.SortOrder)
            .FirstOrDefaultAsync(ct);

        if (featured is null)
        {
            featured = await db.Stories.AsNoTracking()
                .Where(x => x.Published && x.DeletedAt == null)
                .OrderBy(x => x.SortOrder)
                .FirstOrDefaultAsync(ct);
        }

        if (featured is null)
            return null;

        var existing = await db.StoriesOfTheDay.FirstOrDefaultAsync(x => x.PickDate == today, ct);
        if (existing is null)
        {
            db.StoriesOfTheDay.Add(new StoryOfTheDay
            {
                PickDate = today,
                StoryId = featured.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }

        return ToStoryOfDay(today, featured);
    }

    public async Task<WeeklyChallengeDto?> GetActiveChallengeAsync(CancellationToken ct = default)
    {
        var today = TehranTime.TodayTehran();
        var challenge = await db.WeeklyChallenges.AsNoTracking()
            .Where(x => x.IsActive && x.WeekStart <= today && x.WeekEnd >= today)
            .OrderByDescending(x => x.WeekStart)
            .FirstOrDefaultAsync(ct);

        if (challenge is not null)
            return ToChallenge(challenge);

        var seeded = new WeeklyChallenge
        {
            Id = Guid.NewGuid(),
            TitleFa = "چالش این هفته: حیوانات دوست‌داشتنی",
            ThemeTag = "animals",
            DescriptionFa = "نقاشی یک حیوان بکش و قصه‌اش را بساز.",
            WeekStart = StartOfWeekSaturday(today),
            WeekEnd = StartOfWeekSaturday(today).AddDays(6),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.WeeklyChallenges.Add(seeded);
        await db.SaveChangesAsync(ct);
        return ToChallenge(seeded);
    }

    private static DateOnly StartOfWeekSaturday(DateOnly day)
    {
        var daysSinceSaturday = ((int)day.DayOfWeek + 1) % 7;
        return day.AddDays(-daysSinceSaturday);
    }

    public async Task<MemberEngagementDto> GetEngagementAsync(string userId, CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId, ct);
        return ToEngagement(user);
    }

    public async Task<MemberEngagementDto> RecordListenAsync(
        string userId,
        RecordListenRequestDto request,
        CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId, ct);
        if (string.IsNullOrWhiteSpace(request.StoryId))
            throw new ArgumentException("شناسه قصه الزامی است.");

        var today = TehranTime.TodayTehran();
        if (user.LastListenDate != today)
        {
            if (user.LastListenDate == today.AddDays(-1))
                user.ListenStreak += 1;
            else
                user.ListenStreak = 1;
            user.LastListenDate = today;
        }

        user.LastPlayedStoryId = PlainTextSanitizer.Clean(request.StoryId, 64);
        user.LastPlayedPositionSeconds = request.PositionSeconds is >= 0
            ? request.PositionSeconds
            : user.LastPlayedPositionSeconds;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToEngagement(user);
    }

    public async Task RecordCreateActivityAsync(string userId, CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId, ct);
        var today = TehranTime.TodayTehran();
        if (user.LastCreateDate == today)
            return;

        if (user.LastCreateDate == today.AddDays(-1))
            user.CreateStreak += 1;
        else
            user.CreateStreak = 1;

        user.LastCreateDate = today;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<MemberEngagementDto> RedeemPlusAsync(
        string userId,
        RedeemPlusRequestDto request,
        CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId, ct);
        var code = (request.Code ?? string.Empty).Trim();
        if (!PlusCodes.Contains(code))
            throw new ArgumentException("کد Plus نامعتبر است.");

        user.PlanTier = MemberPlans.Plus;
        user.PlusExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToEngagement(user);
    }

    private async Task<AppUser> RequireUserAsync(string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("ورود الزامی است.");
        return await db.AppUsers.FirstOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new KeyNotFoundException("کاربر یافت نشد.");
    }

    private StoryOfTheDayDto ToStoryOfDay(DateOnly date, Story story) => new(
        date,
        story.Id,
        PlainTextSanitizer.Clean(story.TitleFa, 300),
        mediaUrls.Normalize(story.CoverUrl),
        story.DurationSeconds);

    private static WeeklyChallengeDto ToChallenge(WeeklyChallenge c) => new(
        c.Id,
        c.TitleFa,
        c.ThemeTag,
        c.DescriptionFa,
        c.WeekStart,
        c.WeekEnd);

    private static MemberEngagementDto ToEngagement(AppUser user)
    {
        var isPlus = IsPlusActive(user);
        var tier = isPlus ? MemberPlans.Plus : MemberPlans.Free;
        return new MemberEngagementDto(
            user.ListenStreak,
            user.CreateStreak,
            user.LastPlayedStoryId,
            user.LastPlayedPositionSeconds,
            tier,
            user.PlusExpiresAt,
            isPlus,
            !isPlus,
            MemberPlans.DailyCreateLimitFor(tier));
    }

    public static bool IsPlusActive(AppUser user)
    {
        if (!string.Equals(user.PlanTier, MemberPlans.Plus, StringComparison.OrdinalIgnoreCase))
            return false;
        return user.PlusExpiresAt is null || user.PlusExpiresAt > DateTimeOffset.UtcNow;
    }
}
