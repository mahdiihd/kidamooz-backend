namespace Kidamooz.Domain.Entities;

public class StoryAudienceUser
{
    public string StoryId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Story Story { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
