namespace Kidamooz.Domain.Entities;

public class StoryAudienceSegment
{
    public string StoryId { get; set; } = string.Empty;
    public string SegmentId { get; set; } = string.Empty;
    public Story Story { get; set; } = null!;
    public AudienceSegment Segment { get; set; } = null!;
}
