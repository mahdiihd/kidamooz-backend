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
    private readonly CoverGenerationOptions _options = options.Value;

    public async Task<string?> GenerateAsync(Guid storyId, string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return null;

        var stopwatch = Stopwatch.StartNew();
        var encodedPrompt = WebUtility.UrlEncode(prompt.Trim());
        var legacyUrl =
            $"{_options.BaseUrl.TrimEnd('/')}/prompt/{encodedPrompt}?width={_options.Width}&height={_options.Height}";
        var authenticatedUrl =
            $"https://gen.pollinations.ai/image/{encodedPrompt}?width={_options.Width}&height={_options.Height}";

        logger.LogInformation(
            "Cover generation started for {StoryId}. PromptLength={PromptLength}",
            storyId,
            prompt.Length);

        byte[]? imageBytes = null;
        string? sourceUrl = null;

        imageBytes = await DownloadAsync(legacyUrl, useApiKey: false, ct);
        if (imageBytes is { Length: > 0 })
        {
            sourceUrl = legacyUrl;
        }
        else if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            imageBytes = await DownloadAsync(authenticatedUrl, useApiKey: true, ct);
            sourceUrl = authenticatedUrl;
        }

        if (imageBytes is not { Length: > 0 })
        {
            stopwatch.Stop();
            logger.LogWarning(
                "Cover generation failed for {StoryId} after {DurationMs}ms. LegacyUrl={LegacyUrl}",
                storyId,
                stopwatch.ElapsedMilliseconds,
                legacyUrl);
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
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("KidamoozCoverBot/1.0");
            if (useApiKey && !string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _options.ApiKey.Trim());
            }

            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Pollinations cover request failed: {Status} Host={Host}",
                    (int)response.StatusCode,
                    request.RequestUri?.Host);
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
