namespace Kidamooz.Domain.Entities;

public class StoryOfTheDay
{
    public DateOnly PickDate { get; set; }
    public string StoryId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Story? Story { get; set; }
}
