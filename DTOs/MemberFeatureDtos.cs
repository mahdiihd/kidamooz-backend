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

public record AdminStoryOfTheDayDto(
    DateOnly PickDate,
    string StoryId,
    string TitleFa,
    bool IsValid);

public record SetStoryOfTheDayRequestDto(string StoryId);

public record WeeklyChallengeDto(
    Guid Id,
    string TitleFa,
    string ThemeTag,
    string DescriptionFa,
    DateOnly WeekStart,
    DateOnly WeekEnd);

public record WeeklyChallengeAdminDto(
    Guid Id,
    string TitleFa,
    string ThemeTag,
    string DescriptionFa,
    DateOnly WeekStart,
    DateOnly WeekEnd,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record UpsertWeeklyChallengeRequestDto(
    string TitleFa,
    string ThemeTag,
    string DescriptionFa,
    DateOnly WeekStart,
    DateOnly? WeekEnd,
    bool IsActive = true);

public record SetChallengeActiveRequestDto(bool IsActive);

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

public record CreditPackageDto(string ProductId, long AmountTomans, string TitleFa);

public record MemberWalletDto(
    long CreditBalance,
    bool IsPlus,
    bool FreeAiCoverUsed,
    long CoverPriceTomans,
    IReadOnlyList<CreditPackageDto> Packages);

public record ConfirmBazaarPurchaseRequestDto(
    string ProductId,
    string PurchaseToken,
    string? OrderId,
    string? PackageName,
    string? PurchaseData,
    string? DataSignature);

public record AdminGrantCreditRequestDto(long AmountTomans, string? Note);

