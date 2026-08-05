using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Kidamooz.Infrastructure.Ai;

public class GeminiCoverImageGenerator(
    GeminiSettings settings,
    IHttpClientFactory httpClientFactory,
    ILogger<GeminiCoverImageGenerator> logger) : ICoverImageGenerator
{
    public async Task<byte[]?> GenerateAsync(string coverPrompt, CancellationToken ct = default)
    {
        if (!settings.IsConfigured || string.IsNullOrWhiteSpace(coverPrompt))
            return null;

        var model = string.IsNullOrWhiteSpace(settings.CoverImageModel)
            ? "gemini-2.5-flash-image"
            : settings.CoverImageModel.Trim();
        var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? "https://generativelanguage.googleapis.com"
            : settings.BaseUrl.TrimEnd('/');
        var url =
            $"{baseUrl}/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(settings.ApiKey)}";

        var prompt = $"""
            Create one children's book cover illustration.
            Style: colorful, joyful, soft lighting, gentle characters, picture-book look.
            Absolutely no text, letters, watermark, logo, or signature in the image.
            Subject: {coverPrompt.Trim()}
            """;

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                responseModalities = new[] { "TEXT", "IMAGE" }
            }
        };

        try
        {
            var client = httpClientFactory.CreateClient("gemini");
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Gemini cover generation failed: {Status} {Body}",
                    (int)response.StatusCode,
                    Truncate(body));
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            var image = ExtractImageBytes(doc.RootElement);
            if (image is not { Length: > 0 })
            {
                logger.LogWarning("Gemini cover response had no image bytes");
                return null;
            }

            return image;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Gemini cover generation error");
            return null;
        }
    }

    private static byte[]? ExtractImageBytes(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var candidate in candidates.EnumerateArray())
        {
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in parts.EnumerateArray())
            {
                if (TryReadInlineData(part, "inlineData", out var bytes) ||
                    TryReadInlineData(part, "inline_data", out bytes))
                {
                    return bytes;
                }
            }
        }

        return null;
    }

    private static bool TryReadInlineData(JsonElement part, string propertyName, out byte[]? bytes)
    {
        bytes = null;
        if (!part.TryGetProperty(propertyName, out var inline) ||
            inline.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!inline.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.String)
            return false;

        var raw = data.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        try
        {
            bytes = Convert.FromBase64String(raw);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Truncate(string value) =>
        value.Length <= 400 ? value : value[..400];
}
