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
        You are a children's story writer. The image is a child's drawing.
        Create a short bilingual story based on that drawing.

        Rules:
        - titleFa, descriptionFa, storyScript MUST be Persian (Farsi) WITHOUT Arabic diacritics (no tashkeel/harakat)
        - titleEn, descriptionEn MUST be natural English (Latin script only). Never copy Persian into English fields.
        - Suitable for ages 3–8
        - Gentle, joyful, no violence or fear
        - storyScript is for reading aloud by a parent/child, about 1–2 minutes (roughly 180–350 Persian words)
        - coverPrompt in English for a children's book illustration inspired by this drawing: colorful, joyful, children's book illustration style, no text on the image

        Return ONLY raw JSON with no markdown:
        {"titleFa":"...","descriptionFa":"...","titleEn":"...","descriptionEn":"...","storyScript":"...","coverPrompt":"..."}
        Plain text only; no HTML, links, or scripts.
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
        var parsed = await RequestStoryJsonAsync(
            [
                new { text = Prompt },
                new
                {
                    inline_data = new
                    {
                        mime_type = mime,
                        data = Convert.ToBase64String(bytes)
                    }
                }
            ],
            temperature: 0.8,
            failureMessage: "تولید قصه با هوش مصنوعی ناموفق بود. کمی بعد دوباره تلاش کنید.",
            ct);

        return ToContent(parsed);
    }

    public async Task<GeneratedStoryContent> RewriteAsync(
        string titleFa,
        string descriptionFa,
        string storyScript,
        string mode,
        CancellationToken ct = default)
    {
        if (!settings.IsConfigured)
            throw new InvalidOperationException("Gemini API key is not configured.");

        var modeHint = string.Equals(mode, "shorter", StringComparison.OrdinalIgnoreCase)
            ? "متن را کوتاه‌تر و روان‌تر کن (حدود ۱ دقیقه خواندن)."
            : "متن را بازنویسی کن؛ شادتر، واضح‌تر و مناسب‌تر برای کودک ۳ تا ۸ سال.";

        var prompt = $"""
            تو یک نویسنده‌ی قصه‌های کودکانه هستی.
            {modeHint}
            قوانین:
            - titleFa, descriptionFa, storyScript به فارسی بدون اعراب
            - titleEn, descriptionEn انگلیسی طبیعی با حروف لاتین؛ هرگز فارسی را در فیلد انگلیسی کپی نکن
            - ملایم، شاد، بدون خشونت و ترس
            - coverPrompt انگلیسی برای تصویرگری کتاب کودک
            فقط JSON خام با کلیدهای titleFa, descriptionFa, titleEn, descriptionEn, storyScript, coverPrompt

            عنوان فعلی: {titleFa}
            توضیح فعلی: {descriptionFa}
            متن فعلی:
            {storyScript}
            """;

        var parsed = await RequestStoryJsonAsync(
            [new { text = prompt }],
            temperature: 0.7,
            failureMessage: "بازنویسی قصه ناموفق بود. کمی بعد دوباره تلاش کنید.",
            ct);

        return ToContent(parsed);
    }

    private async Task<GeminiStoryJson> RequestStoryJsonAsync(
        object[] parts,
        double temperature,
        string failureMessage,
        CancellationToken ct)
    {
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
                new { parts }
            },
            generationConfig = new
            {
                temperature,
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
            throw new InvalidOperationException(failureMessage);
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

        return parsed;
    }

    public async Task<(string TitleEn, string DescriptionEn)> EnsureEnglishAsync(
        string titleFa,
        string descriptionFa,
        string? titleEn,
        string? descriptionEn,
        CancellationToken ct = default)
    {
        var cleanTitleEn = PlainTextSanitizer.Clean(titleEn, 300);
        var cleanDescriptionEn = PlainTextSanitizer.Clean(descriptionEn, 2000);
        if (IsUsableEnglish(cleanTitleEn, titleFa) && IsUsableEnglish(cleanDescriptionEn, descriptionFa))
            return (cleanTitleEn, cleanDescriptionEn);

        if (!settings.IsConfigured)
        {
            return (
                IsUsableEnglish(cleanTitleEn, titleFa) ? cleanTitleEn : "Children's story",
                IsUsableEnglish(cleanDescriptionEn, descriptionFa) ? cleanDescriptionEn : "A gentle story for kids.");
        }

        var prompt =
            "Translate this Persian children's story metadata into natural English.\n" +
            "Return ONLY raw JSON with keys titleEn and descriptionEn.\n" +
            "Use Latin script only. Do not copy Persian text.\n" +
            $"titleFa: {titleFa}\n" +
            $"descriptionFa: {descriptionFa}";

        try
        {
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
                    new { parts = new object[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    responseMimeType = "application/json"
                }
            };

            var client = httpClientFactory.CreateClient("gemini");
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("ترجمه انگلیسی ناموفق بود.");

            using var doc = JsonDocument.Parse(body);
            var text = ExtractText(doc.RootElement);
            var json = ExtractJsonObject(text);
            var parsed = JsonSerializer.Deserialize<GeminiEnglishJson>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new GeminiEnglishJson();

            var ensuredTitle = PlainTextSanitizer.Clean(
                string.IsNullOrWhiteSpace(parsed.TitleEn) ? cleanTitleEn : parsed.TitleEn,
                300);
            var ensuredDescription = PlainTextSanitizer.Clean(
                string.IsNullOrWhiteSpace(parsed.DescriptionEn) ? cleanDescriptionEn : parsed.DescriptionEn,
                2000);

            if (!IsUsableEnglish(ensuredTitle, titleFa))
                ensuredTitle = "Children's story";
            if (!IsUsableEnglish(ensuredDescription, descriptionFa))
                ensuredDescription = "A gentle story for kids.";

            return (ensuredTitle, ensuredDescription);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "EnsureEnglishAsync failed; using safe fallbacks");
            return (
                IsUsableEnglish(cleanTitleEn, titleFa) ? cleanTitleEn : "Children's story",
                IsUsableEnglish(cleanDescriptionEn, descriptionFa) ? cleanDescriptionEn : "A gentle story for kids.");
        }
    }

    private static GeneratedStoryContent ToContent(GeminiStoryJson parsed)
    {
        var coverPrompt = string.IsNullOrWhiteSpace(parsed.CoverPrompt)
            ? $"Children's book illustration based on a child's drawing titled {parsed.TitleFa}, colorful, joyful, no text"
            : parsed.CoverPrompt;

        var titleFa = PlainTextSanitizer.Clean(parsed.TitleFa, 300);
        var descriptionFa = PlainTextSanitizer.Clean(
            string.IsNullOrWhiteSpace(parsed.DescriptionFa) ? parsed.TitleFa : parsed.DescriptionFa,
            2000);
        var storyScript = PlainTextSanitizer.Clean(parsed.StoryScript, 8000);

        var titleEn = PlainTextSanitizer.Clean(parsed.TitleEn, 300);
        var descriptionEn = PlainTextSanitizer.Clean(parsed.DescriptionEn, 2000);
        if (!IsUsableEnglish(titleEn, titleFa))
            titleEn = string.Empty;
        if (!IsUsableEnglish(descriptionEn, descriptionFa))
            descriptionEn = string.Empty;

        return new GeneratedStoryContent(
            titleFa,
            descriptionFa,
            titleEn,
            descriptionEn,
            storyScript,
            PlainTextSanitizer.Clean(coverPrompt, 1000));
    }

    private static bool IsUsableEnglish(string? value, string persianReference)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (string.Equals(value.Trim(), persianReference.Trim(), StringComparison.Ordinal))
            return false;
        return !ContainsPersianLetters(value);
    }

    private static bool ContainsPersianLetters(string value)
    {
        foreach (var ch in value)
        {
            if (ch is >= '\u0600' and <= '\u06FF' or >= '\u0750' and <= '\u077F' or >= '\uFB50' and <= '\uFDFF' or >= '\uFE70' and <= '\uFEFF')
                return true;
        }

        return false;
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

        [JsonPropertyName("titleEn")]
        public string? TitleEn { get; set; }

        [JsonPropertyName("descriptionEn")]
        public string? DescriptionEn { get; set; }

        [JsonPropertyName("storyScript")]
        public string StoryScript { get; set; } = string.Empty;

        [JsonPropertyName("coverPrompt")]
        public string? CoverPrompt { get; set; }
    }

    private sealed class GeminiEnglishJson
    {
        [JsonPropertyName("titleEn")]
        public string? TitleEn { get; set; }

        [JsonPropertyName("descriptionEn")]
        public string? DescriptionEn { get; set; }
    }
}
