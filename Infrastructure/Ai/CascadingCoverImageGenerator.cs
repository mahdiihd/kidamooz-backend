namespace Kidamooz.Infrastructure.Ai;

public class CascadingCoverImageGenerator(
    GeminiCoverImageGenerator gemini,
    PollinationsCoverImageGenerator pollinations,
    ILogger<CascadingCoverImageGenerator> logger) : ICoverImageGenerator
{
    public async Task<byte[]?> GenerateAsync(string coverPrompt, CancellationToken ct = default)
    {
        var fromGemini = await gemini.GenerateAsync(coverPrompt, ct);
        if (fromGemini is { Length: > 0 })
            return fromGemini;

        logger.LogWarning("Gemini cover unavailable; trying Pollinations fallback");
        return await pollinations.GenerateAsync(coverPrompt, ct);
    }
}
