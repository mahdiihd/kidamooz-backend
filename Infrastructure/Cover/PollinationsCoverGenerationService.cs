using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Kidamooz.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Kidamooz.Infrastructure.Cover;

public class PollinationsCoverGenerationService(
    IHttpClientFactory httpClientFactory,
    IMediaStorageService storage,
    IWebHostEnvironment environment,
    IOptions<CoverGenerationOptions> options,
    ILogger<PollinationsCoverGenerationService> logger) : ICoverGenerationService
{
    private const int MaxPromptLength = 320;
    private readonly CoverGenerationOptions _options = options.Value;

    public async Task<string?> GenerateAsync(Guid storyId, string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return null;

        var stopwatch = Stopwatch.StartNew();
        var trimmedPrompt = prompt.Trim();
        if (trimmedPrompt.Length > MaxPromptLength)
            trimmedPrompt = trimmedPrompt[..MaxPromptLength].TrimEnd() + "…";

        var encodedPrompt = WebUtility.UrlEncode(trimmedPrompt);
        var seed = Random.Shared.Next(1, 999_999);
        var legacyUrl =
            $"{_options.BaseUrl.TrimEnd('/')}/prompt/{encodedPrompt}?width={_options.Width}&height={_options.Height}&model={_options.Model}&seed={seed}&nologo=true";
        var authenticatedUrl =
            $"https://gen.pollinations.ai/image/{encodedPrompt}?width={_options.Width}&height={_options.Height}&model={_options.Model}&seed={seed}&nologo=true";

        logger.LogInformation(
            "Cover generation started for {StoryId}. PromptLength={PromptLength}",
            storyId,
            trimmedPrompt.Length);

        byte[]? imageBytes = null;
        string? sourceUrl = null;

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            imageBytes = await DownloadAsync(authenticatedUrl, useApiKey: true, ct);
            if (imageBytes is { Length: > 0 })
                sourceUrl = authenticatedUrl;
        }

        if (imageBytes is not { Length: > 0 })
        {
            imageBytes = await DownloadAsync(legacyUrl, useApiKey: false, ct);
            if (imageBytes is { Length: > 0 })
                sourceUrl = legacyUrl;
        }

        if (imageBytes is not { Length: > 0 })
        {
            stopwatch.Stop();
            logger.LogWarning(
                "Cover generation failed for {StoryId} after {DurationMs}ms",
                storyId,
                stopwatch.ElapsedMilliseconds);
            return null;
        }

        await SaveLocalCopyAsync(storyId, imageBytes, ct);

        await using var stream = new MemoryStream(imageBytes);
        var publicUrl = await storage.UploadAsync(
            stream,
            $"{storyId:N}.png",
            "image/png",
            "cover",
            ct);

        stopwatch.Stop();
        logger.LogInformation(
            "Cover generation succeeded for {StoryId} in {DurationMs}ms. SourceUrl={SourceUrl} PublicUrl={PublicUrl}",
            storyId,
            stopwatch.ElapsedMilliseconds,
            sourceUrl,
            publicUrl);

        return publicUrl;
    }

    private async Task<byte[]?> DownloadAsync(string url, bool useApiKey, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("cover-generation");
            var requestUrl = url;
            if (useApiKey && !string.IsNullOrWhiteSpace(_options.ApiKey))
                requestUrl = AppendQuery(requestUrl, "key", _options.ApiKey.Trim());

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.UserAgent.ParseAdd("KidamoozCoverBot/1.0");
            if (useApiKey && !string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _options.ApiKey.Trim());
            }

            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "Pollinations cover request failed: {Status} Host={Host} Body={Body}",
                    (int)response.StatusCode,
                    request.RequestUri?.Host,
                    Truncate(body, 240));
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            return bytes.Length > 0 ? bytes : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Pollinations cover download error for {Host}", new Uri(url).Host);
            return null;
        }
    }

    private static string AppendQuery(string url, string key, string value) =>
        url.Contains('?', StringComparison.Ordinal)
            ? $"{url}&{key}={WebUtility.UrlEncode(value)}"
            : $"{url}?{key}={WebUtility.UrlEncode(value)}";

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private async Task SaveLocalCopyAsync(Guid storyId, byte[] imageBytes, CancellationToken ct)
    {
        try
        {
            var folder = Path.GetFullPath(
                Path.Combine(environment.ContentRootPath, _options.OutputFolder));
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"{storyId:N}.png");
            await File.WriteAllBytesAsync(path, imageBytes, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save local cover copy for {StoryId}", storyId);
        }
    }
}
