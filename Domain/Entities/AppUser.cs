namespace Kidamooz.Domain.Entities;

public class AppUser
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<StoryAudienceUser> StoryAudienceUsers { get; set; } = [];
    public ICollection<StoryDraft> StoryDrafts { get; set; } = [];
}
