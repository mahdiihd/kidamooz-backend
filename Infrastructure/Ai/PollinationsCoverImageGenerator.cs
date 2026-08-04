using System.Net;
using System.Net.Http.Headers;

namespace Kidamooz.Infrastructure.Ai;

public class PollinationsCoverImageGenerator(
    IHttpClientFactory httpClientFactory,
    CoverImageSettings settings,
    ILogger<PollinationsCoverImageGenerator> logger) : ICoverImageGenerator
{
    public async Task<byte[]?> GenerateAsync(string coverPrompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(coverPrompt) || string.IsNullOrWhiteSpace(settings.ApiKey))
            return null;

        var prompt =
            $"{coverPrompt.Trim()}, children's book illustration, colorful, joyful, soft lighting, no text, no watermark";
        var encoded = WebUtility.UrlEncode(prompt);
        var seed = Random.Shared.Next(1, 999_999);
        var url =
            $"https://gen.pollinations.ai/image/{encoded}?width=768&height=1024&model=flux&seed={seed}";

        try
        {
            var client = httpClientFactory.CreateClient("cover-image");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("KidamoozCoverBot/1.0");
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());

            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Cover image generation failed: {Status} urlHost={Host}",
                    (int)response.StatusCode,
                    request.RequestUri?.Host);
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
