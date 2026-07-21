using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kidamooz.Infrastructure.Security;

namespace Kidamooz.Infrastructure.Ai;

public class GeminiStoryClient(
    GeminiSettings settings,
    IHttpClientFactory httpClientFactory,
    ILogger<GeminiStoryClient> logger) : IGeminiStoryClient
{
    private const string Prompt = """
        تو یک نویسنده‌ی قصه‌های کودکانه فارسی هستی.
        تصویر یک نقاشی کودکانه است. بر اساس همان نقاشی یک قصه کوتاه بساز.

        قوانین:
        - زبان خروجی فقط فارسی
        - مناسب سن ۳ تا ۸ سال
        - ملایم، شاد، بدون خشونت و ترس
        - storyScript برای خواندن بلند با صدای والد/کودک باشد، حدود ۱ تا ۲ دقیقه (حدود ۱۸۰ تا ۳۵۰ کلمه)
        - coverPrompt را انگلیسی بنویس برای تولید تصویرگری کتاب کودک از روی همین نقاشی: رنگارنگ، شاد، سبک children's book illustration، بدون هیچ متن روی تصویر

        فقط یک JSON خام بدون markdown و بدون HTML یا تگ script برگردان با این کلیدها:
        {"titleFa":"...","descriptionFa":"...","storyScript":"...","coverPrompt":"..."}
        متن‌ها باید متن ساده باشند؛ هیچ تگ HTML، لینک خطرناک یا اسکریپت مجاز نیست.
        """;

    public async Task<GeneratedStoryContent> GenerateFromDrawingAsync(
        Stream image,
        string contentType,
        string fileName,
        CancellationToken ct = default)
    {
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Gemini API key is not configured.");

        await using var ms = new MemoryStream();
        await image.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        if (bytes.Length == 0)
            throw new ArgumentException("تصویر نقاشی خالی است.");

        var mime = NormalizeMime(contentType, fileName);
        var model = string.IsNullOrWhiteSpace(settings.Model) ? "gemini-flash-latest" : settings.Model;
        var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
            ? "https://generativelanguage.googleapis.com"
            : settings.BaseUrl.TrimEnd('/');
        var url =
            $"{baseUrl}/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(settings.ApiKey)}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = Prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mime,
                                data = Convert.ToBase64String(bytes)
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.8,
                responseMimeType = "application/json"
            }
        };

        var client = httpClientFactory.CreateClient("gemini");
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Gemini request failed to reach the server");
            throw new InvalidOperationException("سرویس هوش مصنوعی در دسترس نیست. کمی بعد دوباره تلاش کنید.");
        }

        using var _ = response;
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Gemini story generation failed: {Status} {Body}", (int)response.StatusCode, Truncate(body));
            throw new InvalidOperationException("تولید قصه با هوش مصنوعی ناموفق بود. کمی بعد دوباره تلاش کنید.");
        }

        using var doc = JsonDocument.Parse(body);
        var text = ExtractText(doc.RootElement);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("پاسخ هوش مصنوعی خالی بود.");

        var json = ExtractJsonObject(text);
        var parsed = JsonSerializer.Deserialize<GeminiStoryJson>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("پاسخ هوش مصنوعی نامعتبر بود.");

        if (string.IsNullOrWhiteSpace(parsed.TitleFa) || string.IsNullOrWhiteSpace(parsed.StoryScript))
            throw new InvalidOperationException("متن قصه ناقص تولید شد.");

        var coverPrompt = string.IsNullOrWhiteSpace(parsed.CoverPrompt)
            ? $"Children's book illustration based on a child's drawing titled {parsed.TitleFa}, colorful, joyful, no text"
            : parsed.CoverPrompt;

        return new GeneratedStoryContent(
            PlainTextSanitizer.Clean(parsed.TitleFa, 300),
            PlainTextSanitizer.Clean(parsed.DescriptionFa ?? parsed.TitleFa, 2000),
            PlainTextSanitizer.Clean(parsed.StoryScript, 8000),
            PlainTextSanitizer.Clean(coverPrompt, 1000));
    }

    private static string ExtractText(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return string.Empty;

        var content = candidates[0].GetProperty("content");
        if (!content.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0)
            return string.Empty;

        return parts[0].TryGetProperty("text", out var text) ? text.GetString() ?? string.Empty : string.Empty;
    }

    private static string ExtractJsonObject(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = trimmed.IndexOf('\n');
            if (firstNl >= 0)
                trimmed = trimmed[(firstNl + 1)..];
            var fence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (fence >= 0)
                trimmed = trimmed[..fence];
            trimmed = trimmed.Trim();
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException("پاسخ هوش مصنوعی JSON نبود.");

        return trimmed[start..(end + 1)];
    }

    private static string NormalizeMime(string contentType, string fileName)
    {
        var mime = (contentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();
        if (mime is "image/jpeg" or "image/png" or "image/webp")
            return mime;

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    private static string Truncate(string value) =>
        value.Length <= 400 ? value : value[..400];

    private sealed class GeminiStoryJson
    {
        [JsonPropertyName("titleFa")]
        public string TitleFa { get; set; } = string.Empty;

        [JsonPropertyName("descriptionFa")]
        public string? DescriptionFa { get; set; }

        [JsonPropertyName("storyScript")]
        public string StoryScript { get; set; } = string.Empty;

        [JsonPropertyName("coverPrompt")]
        public string? CoverPrompt { get; set; }
    }
}
