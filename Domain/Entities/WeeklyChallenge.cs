namespace Kidamooz.Domain.Entities;

public class WeeklyChallenge
{
    public Guid Id { get; set; }
    public string TitleFa { get; set; } = string.Empty;
    public string ThemeTag { get; set; } = string.Empty;
    public string DescriptionFa { get; set; } = string.Empty;
    public DateOnly WeekStart { get; set; }
    public DateOnly WeekEnd { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
