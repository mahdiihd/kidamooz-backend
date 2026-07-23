namespace Kidamooz.Domain.Entities;

public class MemberFavorite
{
    public string UserId { get; set; } = string.Empty;
    public string StoryId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public AppUser? User { get; set; }
    public Story? Story { get; set; }
}
