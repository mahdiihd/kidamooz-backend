namespace Kidamooz.Domain.Entities;

public class StoryDraft
{
    public Guid Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string Status { get; set; } = StoryDraftStatuses.Generating;
    public string? DrawingUrl { get; set; }
    public string? CoverUrl { get; set; }
    public bool UsedFallbackCover { get; set; }
    public string TitleFa { get; set; } = string.Empty;
    public string DescriptionFa { get; set; } = string.Empty;
    public string StoryScript { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public int? DurationSeconds { get; set; }
    public string? PublishedStoryId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RejectReason { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public AppUser? User { get; set; }
}

public static class StoryDraftStatuses
{
    public const string Generating = "generating";
    public const string Ready = "ready";
    public const string AudioUploaded = "audio_uploaded";
    public const string PendingReview = "pending_review";
    public const string Published = "published";
    public const string Rejected = "rejected";
    public const string Failed = "failed";
}
