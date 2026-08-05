namespace Kidamooz.Infrastructure.Ai;

public class AvalAiSettings
{
    public const string SectionName = "AvalAi";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.avalai.ir/v1";
    public string ImageModel { get; set; } = "imagen-4.0-fast-generate-001";
    public string ImageSize { get; set; } = "1024x1024";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
