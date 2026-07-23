namespace Kidamooz.DTOs;

public record ChildProfileDto(
    Guid Id,
    string Name,
    int Age,
    string AvatarKey,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record UpsertChildProfileRequestDto(
    string Name,
    int Age,
    string? AvatarKey);

public record MemberFavoriteDto(
    string StoryId,
    DateTimeOffset CreatedAt);

public record SyncFavoritesRequestDto(IReadOnlyList<string> StoryIds);

public record SyncFavoritesResponseDto(IReadOnlyList<string> StoryIds);

public record StoryOfTheDayDto(
    DateOnly PickDate,
    string StoryId,
    string TitleFa,
    string? CoverUrl,
    int DurationSeconds);

public record WeeklyChallengeDto(
    Guid Id,
    string TitleFa,
    string ThemeTag,
    string DescriptionFa,
    DateOnly WeekStart,
    DateOnly WeekEnd);

public record MemberEngagementDto(
    int ListenStreak,
    int CreateStreak,
    string? LastPlayedStoryId,
    double? LastPlayedPositionSeconds,
    string PlanTier,
    DateTimeOffset? PlusExpiresAt,
    bool CanDownloadOffline,
    bool AdsEnabled,
    int DailyCreateLimit);

public record RecordListenRequestDto(string StoryId, double? PositionSeconds);

public record RedeemPlusRequestDto(string Code);

public record RewriteStoryDraftRequestDto(string? Mode);

public record RegenerateCoverRequestDto(string? PromptHint);
