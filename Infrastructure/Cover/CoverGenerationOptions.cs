namespace Kidamooz.Infrastructure.Cover;

public class CoverGenerationOptions
{
    public const string SectionName = "CoverGeneration";

    public string BaseUrl { get; set; } = "https://image.pollinations.ai";
    public string? ApiKey { get; set; }
    public string OutputFolder { get; set; } = "wwwroot/covers";
    public int Width { get; set; } = 1024;
    public int Height { get; set; } = 1024;
    public int TimeoutSeconds { get; set; } = 60;
}
