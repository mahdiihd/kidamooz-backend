namespace Kidamooz.Domain.Entities;

public class Story
{
    public string Id { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string TitleFa { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string DescriptionFa { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public string ProgressIcon { get; set; } = "star";
    public int DurationSeconds { get; set; }
    public int AgeMin { get; set; }
    public int AgeMax { get; set; }
    public bool Featured { get; set; }
    public int SortOrder { get; set; }
    public bool Published { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string Visibility { get; set; } = "public";
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<StoryChapter> Chapters { get; set; } = [];
    public ICollection<StoryAudienceSegment> AudienceSegments { get; set; } = [];
    public ICollection<StoryAudienceUser> AudienceUsers { get; set; } = [];
}
