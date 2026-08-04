namespace Kidamooz.Infrastructure.Ai;

public class CascadingCoverImageGenerator(
    GeminiCoverImageGenerator gemini,
    PollinationsCoverImageGenerator pollinations,
    CoverImageSettings coverSettings,
    ILogger<CascadingCoverImageGenerator> logger) : ICoverImageGenerator
{
    public async Task<byte[]?> GenerateAsync(string coverPrompt, CancellationToken ct = default)
    {
        var fromGemini = await gemini.GenerateAsync(coverPrompt, ct);
        if (fromGemini is { Length: > 0 })
            return fromGemini;

        if (string.IsNullOrWhiteSpace(coverSettings.ApiKey))
        {
            logger.LogWarning("Gemini cover unavailable and Pollinations API key is missing; skipping cover AI");
            return null;
        }

        logger.LogWarning("Gemini cover unavailable; trying Pollinations fallback");
        return await pollinations.GenerateAsync(coverPrompt, ct);
    }
}
