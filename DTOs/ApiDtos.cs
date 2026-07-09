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
    bool Featured,
    int SortOrder,
    bool Published,
    DateTimeOffset? PublishedAt,
    StoryAccessDto Access,
    List<StoryChapterDto>? Chapters) : StoryDto(
        Id, Title, Description, CoverUrl, AudioUrl, DurationSeconds, AgeMin, AgeMax,
        CategoryId, Featured, SortOrder, Published, PublishedAt, Access);

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
    bool Featured,
    int SortOrder,
    bool Published,
    DateTimeOffset? PublishedAt,
    List<StoryChapterDto>? Chapters) : PublicStoryDto(
        Id, Title, TitleFa, TitleEn, Description, DescriptionFa, DescriptionEn,
        CoverUrl, AudioUrl, DurationSeconds, AgeMin, AgeMax, CategoryId,
        Featured, SortOrder, Published, PublishedAt);
