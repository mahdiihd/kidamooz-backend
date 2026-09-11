using System.ComponentModel.DataAnnotations;

namespace Kidamooz.DTOs;

public record MemberAuthRequestDto(
    [Required, StringLength(20)] string Mobile,
    [Required, RegularExpression("^[0-9]{6}$")] string Code);
public record RequestMemberOtpDto([Required, StringLength(20)] string Mobile);

public record MemberProfileDto(string Id, string Mobile, string DisplayName, bool ProfileComplete = true);

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
    string TitleEn,
    string DescriptionEn,
    string StoryScript,
    string? ChallengeTag,
    string? AudioUrl,
    string? UploadedAudioUrl,
    int? DurationSeconds,
    string? PublishedStoryId,
    string? ErrorMessage,
    string? RejectReason,
    string? AuthorName,
    string? AuthorMobile,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool CanRemoveFromProfile,
    string CoverChoice = "drawing");

public record StoryDraftQuotaDto(
    bool CanCreateToday,
    int DailyLimit,
    int UsedToday,
    DateTimeOffset? NextAvailableAt,
    string PlanTier,
    bool IsPlus);

public record UpdateStoryDraftRequestDto(
    string? TitleFa,
    string? DescriptionFa,
    string? TitleEn,
    string? DescriptionEn,
    string? StoryScript,
    string? ChallengeTag);

public record RejectStoryDraftRequestDto(string? Reason);

public record ApproveStoryDraftRequestDto(string? PreferredNarration, string? CoverUrl = null);

public record ApproveStoryDraftResponseDto(
    Guid DraftId,
    string StoryId,
    StoryDraftDto Draft);
