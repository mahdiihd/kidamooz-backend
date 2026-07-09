namespace Kidamooz.Domain.Entities;

public class AudienceSegment
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<StoryAudienceSegment> StoryAudienceSegments { get; set; } = [];
}
