using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Kidamooz.Infrastructure.Ai;

public class AvalAiCoverImageGenerator(
    AvalAiSettings settings,
    IHttpClientFactory httpClientFactory,
    ILogger<AvalAiCoverImageGenerator> logger) : ICoverImageGenerator
{
    public async Task<byte[]?> GenerateAsync(string coverPrompt, CancellationToken ct = default)
    {
        if (!settings.IsConfigured || string.IsNullOrWhiteSpace(coverPrompt))
            return null;

        var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? "https://api.avalai.ir/v1"
            : settings.BaseUrl.TrimEnd('/');
        var model = string.IsNullOrWhiteSpace(settings.ImageModel)
            ? "imagen-4.0-fast-generate-001"
            : settings.ImageModel.Trim();
        var size = string.IsNullOrWhiteSpace(settings.ImageSize) ? "1024x1024" : settings.ImageSize.Trim();
        var url = $"{baseUrl}/images/generations";

        var prompt = $"""
            Children's book cover illustration, colorful, joyful, soft lighting, gentle characters, picture-book look.
            Absolutely no text, letters, watermark, logo, or signature in the image.
            Subject: {coverPrompt.Trim()}
            """;

        var payload = new
        {
            model,
            prompt,
            n = 1,
            size,
            response_format = "b64_json"
        };

        try
        {
            var client = httpClientFactory.CreateClient("avalai");
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "AvalAI cover generation failed: {Status} {Body}",
                    (int)response.StatusCode,
                    Truncate(body));
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Array ||
                data.GetArrayLength() == 0)
            {
                logger.LogWarning("AvalAI cover response missing data array");
                return null;
            }

            var first = data[0];
            if (!first.TryGetProperty("b64_json", out var b64) || b64.ValueKind != JsonValueKind.String)
            {
                logger.LogWarning("AvalAI cover response missing b64_json");
                return null;
            }

            var raw = b64.GetString();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var bytes = Convert.FromBase64String(raw);
            return bytes.Length > 0 ? bytes : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "AvalAI cover generation error");
            return null;
        }
    }

    private static string Truncate(string value) =>
        value.Length <= 400 ? value : value[..400];
}
