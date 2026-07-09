namespace Kidamooz.Domain.Entities;

public class Category
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string TitleFa { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool Published { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<Story> Stories { get; set; } = [];
}
