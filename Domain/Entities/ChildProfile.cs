namespace Kidamooz.Domain.Entities;

public class ChildProfile
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; } = 5;
    public string AvatarKey { get; set; } = "moon";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public AppUser? User { get; set; }
}
