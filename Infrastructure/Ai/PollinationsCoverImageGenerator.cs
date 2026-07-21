using System.Net;

namespace Kidamooz.Infrastructure.Ai;

public class PollinationsCoverImageGenerator(
    IHttpClientFactory httpClientFactory,
    CoverImageSettings settings,
    ILogger<PollinationsCoverImageGenerator> logger) : ICoverImageGenerator
{
    public async Task<byte[]?> GenerateAsync(string coverPrompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(coverPrompt))
            return null;

        var prompt =
            $"{coverPrompt.Trim()}, children's book illustration, colorful, joyful, soft lighting, no text, no watermark";
        var encoded = WebUtility.UrlEncode(prompt);
        var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? "https://image.pollinations.ai"
            : settings.BaseUrl.TrimEnd('/');
        var url =
            $"{baseUrl}/prompt/{encoded}?width=768&height=1024&nologo=true&model=flux&seed={Random.Shared.Next(1, 999999)}";

        try
        {
            var client = httpClientFactory.CreateClient("cover-image");
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Cover image generation failed: {Status}", (int)response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            return bytes.Length > 0 ? bytes : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Cover image generation error");
            return null;
        }
    }
}
