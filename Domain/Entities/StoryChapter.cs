namespace Kidamooz.Domain.Entities;

public class StoryChapter
{
    public Guid Id { get; set; }
    public string StoryId { get; set; } = string.Empty;
    public string TitleFa { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public int StartSeconds { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Story Story { get; set; } = null!;
}
