namespace Kidamooz.Infrastructure.Ai;

public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-flash-latest";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
