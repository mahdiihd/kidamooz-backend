namespace Kidamooz.DTOs;

public record MemberAuthRequestDto(string Mobile, string Password, string? DisplayName);

public record MemberProfileDto(string Id, string Mobile, string DisplayName);

public record MemberAuthResponseDto(string AccessToken, MemberProfileDto User);

public record UpdateMemberProfileRequestDto(string? DisplayName);

public record StoryDraftDto(
    Guid Id,
    string Status,
    string? DrawingUrl,
    string? CoverUrl,
    bool UsedFallbackCover,
    string TitleFa,
    string DescriptionFa,
    string StoryScript,
    string? AudioUrl,
    int? DurationSeconds,
    string? PublishedStoryId,
    string? ErrorMessage,
    string? RejectReason,
    string? AuthorName,
    string? AuthorMobile,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record StoryDraftQuotaDto(
    bool CanCreateToday,
    int DailyLimit,
    int UsedToday,
    DateTimeOffset? NextAvailableAt);

public record UpdateStoryDraftRequestDto(
    string? TitleFa,
    string? DescriptionFa,
    string? StoryScript);

public record RejectStoryDraftRequestDto(string? Reason);

public record ApproveStoryDraftResponseDto(
    Guid DraftId,
    string StoryId,
    StoryDraftDto Draft);
