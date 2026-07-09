namespace Kidamooz.Domain.Entities;

public class StoryViewsDaily
{
    public DateOnly ViewDate { get; set; }
    public string StoryId { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public Story Story { get; set; } = null!;
}
