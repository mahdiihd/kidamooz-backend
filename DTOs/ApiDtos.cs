using Microsoft.AspNetCore.Http;

namespace Kidamooz.DTOs;

public record LocalizedTextDto(string Fa, string En);

public record StoryAudienceDto(List<string> SegmentIds, List<string> UserIds);

public record StoryAccessDto(string Visibility, StoryAudienceDto Audience);

public record StoryChapterDto(LocalizedTextDto Title, int StartSeconds, string ImageUrl);

public record StoryDto(
    string Id,
    LocalizedTextDto Title,
    LocalizedTextDto Description,
    string CoverUrl,
    string AudioUrl,
    int DurationSeconds,
    int AgeMin,
    int AgeMax,
    string CategoryId,
    string ProgressIcon,
    bool Featured,
    int SortOrder,
    bool Published,
    DateTimeOffset? PublishedAt,
    StoryAccessDto Access);

public record StoryDetailDto(
    string Id,
    LocalizedTextDto Title,
    LocalizedTextDto Description,
    string CoverUrl,
    string AudioUrl,
    int DurationSeconds,
    int AgeMin,
    int AgeMax,
    string CategoryId,
    string ProgressIcon,
    bool Featured,
    int SortOrder,
    bool Published,
    DateTimeOffset? PublishedAt,
    StoryAccessDto Access,
    List<StoryChapterDto>? Chapters) : StoryDto(
        Id, Title, Description, CoverUrl, AudioUrl, DurationSeconds, AgeMin, AgeMax,
        CategoryId, ProgressIcon, Featured, SortOrder, Published, PublishedAt, Access);

public record StoryListResponseDto(List<StoryDto> Items, int Total);

public record StoryPayloadDto(
    string? Id,
    LocalizedTextDto Title,
    LocalizedTextDto Description,
    string CoverUrl,
    string AudioUrl,
    int DurationSeconds,
    int AgeMin,
    int AgeMax,
    string CategoryId,
    string ProgressIcon,
    bool Featured,
    int SortOrder,
    bool Published,
    DateTimeOffset? PublishedAt,
    StoryAccessDto Access);

public record CategoryDto(
    string Id,
    LocalizedTextDto Title,
    string Slug,
    string IconUrl,
    string Color,
    int SortOrder,
    bool Published);

public record CategoryPayloadDto(
    string? Id,
    LocalizedTextDto Title,
    string Slug,
    string IconUrl,
    string Color,
    int SortOrder,
    bool Published);

public record AuthTokensDto(string AccessToken, string RefreshToken);

public record LoginRequestDto(string Email, string Password);

public record RefreshRequestDto(string RefreshToken);

public record CreateAdminUserRequestDto(string Email, string Password, string DisplayName, string Role = "editor");

public record ResetAdminPasswordRequestDto(string Password);

public record AdminUserDto(
    string Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record PublishRequestDto(bool Published);

public record FeaturedRequestDto(bool Featured);

public record ReorderRequestDto(List<string> Ids);

public record ChaptersRequestDto(List<StoryChapterDto> Chapters);

public record CatalogVersionDto(string Version, DateTimeOffset UpdatedAt);

public record DashboardViewsDto(int Total, int Today, int ThisWeek);

public record DashboardStatsDto(
    int TotalStories,
    int PublishedStories,
    int DraftStories,
    int FeaturedStories,
    int TotalCategories,
    int PublishedCategories,
    DashboardViewsDto Views,
    CatalogVersionDto CatalogVersion,
    DateTimeOffset? LastPublishedAt);

public record AudienceSegmentDto(string Id, string Label, string Description);

public record AudienceUserDto(string Id, string Label, string Email);

public record UploadUrlRequestDto(string FileName, string ContentType, string MediaType);

public record UploadUrlResponseDto(string UploadUrl, string PublicUrl, DateTimeOffset ExpiresAt);

public record ConfirmUploadRequestDto(string PublicUrl, string MediaType);

public record ConfirmUploadResponseDto(string Url);

public class CategorySaveForm
{
    public string? Id { get; set; }
    public string TitleFa { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string Color { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool Published { get; set; }
    public IFormFile? Icon { get; set; }

    public CategoryPayloadDto ToPayload(string iconUrl) =>
        new(Id, new LocalizedTextDto(TitleFa, TitleEn), Slug, iconUrl, Color, SortOrder, Published);
}

public class StorySaveForm
{
    public string? Id { get; set; }
    public string TitleFa { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string DescriptionFa { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public string? AudioUrl { get; set; }
    public int DurationSeconds { get; set; }
    public int AgeMin { get; set; }
    public int AgeMax { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string ProgressIcon { get; set; } = "star";
    public bool Featured { get; set; }
    public int SortOrder { get; set; }
    public bool Published { get; set; }
    public string AccessJson { get; set; } = "{}";
    public string ChaptersJson { get; set; } = "[]";
    public IFormFile? Cover { get; set; }
    public IFormFile? Audio { get; set; }
}

public record AuditLogDto(
    string Id,
    string Action,
    string EntityType,
    string EntityId,
    string EntityTitle,
    string ActorEmail,
    [property: System.Text.Json.Serialization.JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    string? Details);

public record PublicStoryListResponseDto(List<PublicStoryDto> Items, int Total);

public record PublicStoryDto(
    string Id,
    string Title,
    string TitleFa,
    string TitleEn,
    string Description,
    string DescriptionFa,
    string DescriptionEn,
    string CoverUrl,
    string AudioUrl,
    int DurationSeconds,
    int AgeMin,
    int AgeMax,
    string CategoryId,
    string ProgressIcon,
    bool Featured,
    int SortOrder,
    bool Published,
    DateTimeOffset? PublishedAt);

public record PublicStoryDetailDto(
    string Id,
    string Title,
    string TitleFa,
    string TitleEn,
    string Description,
    string DescriptionFa,
    string DescriptionEn,
    string CoverUrl,
    string AudioUrl,
    int DurationSeconds,
    int AgeMin,
    int AgeMax,
    string CategoryId,
    string ProgressIcon,
    bool Featured,
    int SortOrder,
    bool Published,
    DateTimeOffset? PublishedAt,
    List<StoryChapterDto>? Chapters) : PublicStoryDto(
        Id, Title, TitleFa, TitleEn, Description, DescriptionFa, DescriptionEn,
        CoverUrl, AudioUrl, DurationSeconds, AgeMin, AgeMax, CategoryId, ProgressIcon,
        Featured, SortOrder, Published, PublishedAt);

public record DeviceRegisterRequestDto(string Token, string Platform = "android", string? AppVersion = null, string? UserId = null);

public record DeviceUnregisterRequestDto(string Token);

public record NotificationDataDto(string? StoryId);

public record BroadcastNotificationRequestDto(
    string Title,
    string Body,
    string Audience = "all",
    List<string>? UserIds = null,
    NotificationDataDto? Data = null);

public record BroadcastNotificationResponseDto(int TotalTokens, int SuccessCount, int FailureCount);
