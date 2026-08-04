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
        if (string.IsNullOrWhiteSpace(coverPrompt))
            return null;

        var prompt =
            $"{coverPrompt.Trim()}, children's book illustration, colorful, joyful, soft lighting, no text, no watermark";
        var encoded = WebUtility.UrlEncode(prompt);
        var seed = Random.Shared.Next(1, 999_999);
        var client = httpClientFactory.CreateClient("cover-image");

        foreach (var url in BuildCandidateUrls(encoded, seed))
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("KidamoozCoverBot/1.0");
                if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());
                }

                using var response = await client.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Cover image generation failed: {Status} urlHost={Host}",
                        (int)response.StatusCode,
                        request.RequestUri?.Host);
                    if ((int)response.StatusCode == 429)
                        await Task.Delay(1200, ct);
                    continue;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                if (bytes.Length > 0)
                    return bytes;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cover image generation error");
            }
        }

        return null;
    }

    private IEnumerable<string> BuildCandidateUrls(string encodedPrompt, int seed)
    {
        var legacyBase = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? "https://image.pollinations.ai"
            : settings.BaseUrl.TrimEnd('/');

        yield return
            $"{legacyBase}/prompt/{encodedPrompt}?width=768&height=1024&nologo=true&model=flux&seed={seed}";

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            yield return
                $"https://gen.pollinations.ai/image/{encodedPrompt}?width=768&height=1024&model=flux&seed={seed}";
        }
    }
}
