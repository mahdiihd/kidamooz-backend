using Amazon.S3;
using Amazon.S3.Model;
using Kidamooz.DTOs;

namespace Kidamooz.Infrastructure.Storage;

public interface IMediaStorageService
{
    UploadUrlResponseDto CreateUploadUrl(string fileName, string contentType, string mediaType);
    Task<string> UploadAsync(Stream content, string fileName, string contentType, string mediaType, CancellationToken ct = default);
    Task<bool> ObjectExistsAsync(string publicUrl, CancellationToken ct = default);
    bool ValidatePublicUrl(string publicUrl, string mediaType);
}

public class LiaraMediaStorageService(IAmazonS3 s3, LiaraSettings settings) : IMediaStorageService
{
    private static readonly HashSet<string> ImageTypes = ["image/webp", "image/jpeg", "image/png"];
    private static readonly HashSet<string> AudioTypes =
    [
        "audio/mpeg",
        "audio/mp4",
        "audio/aac",
        "audio/m4a",
        "audio/x-m4a",
        "audio/webm",
        "audio/wav",
        "audio/ogg"
    ];

    public UploadUrlResponseDto CreateUploadUrl(string fileName, string contentType, string mediaType)
    {
        ValidateContentType(contentType, mediaType);

        var key = BuildObjectKey(mediaType, fileName);
        var expires = DateTime.UtcNow.AddMinutes(15);

        var uploadUrl = s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = settings.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = expires
        });

        return new UploadUrlResponseDto(uploadUrl, BuildPublicUrl(key), new DateTimeOffset(expires, TimeSpan.Zero));
    }

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string mediaType,
        CancellationToken ct = default)
    {
        contentType = NormalizeContentType(contentType, fileName);
        ValidateContentType(contentType, mediaType);

        var key = BuildObjectKey(mediaType, fileName);
        var request = new PutObjectRequest
        {
            BucketName = settings.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            UseChunkEncoding = false,
            DisableDefaultChecksumValidation = true
        };

        try
        {
            await s3.PutObjectAsync(request, ct);
        }
        catch (AmazonS3Exception ex)
        {
            throw new MediaStorageException($"آپلود به فضای ذخیره‌سازی ناموفق بود: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not MediaStorageException and not OperationCanceledException)
        {
            throw new MediaStorageException($"آپلود به فضای ذخیره‌سازی ناموفق بود: {ex.Message}", ex);
        }

        return BuildPublicUrl(key);
    }

    public async Task<bool> ObjectExistsAsync(string publicUrl, CancellationToken ct = default)
    {
        var key = ExtractObjectKey(publicUrl);
        if (key is null)
            return false;

        return await ObjectExistsByKeyAsync(key, ct);
    }

    public bool ValidatePublicUrl(string publicUrl, string mediaType)
    {
        var key = ExtractObjectKey(publicUrl);
        if (key is null)
            return false;

        var prefix = mediaType switch
        {
            "cover" => "covers/",
            "audio" => "audio/",
            "icon" => "icons/",
            "drawing" => "drawings/",
            _ => null
        };

        return prefix != null && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> ObjectExistsByKeyAsync(string key, CancellationToken ct)
    {
        try
        {
            await s3.GetObjectMetadataAsync(settings.BucketName, key, ct);
            return true;
        }
        catch (AmazonS3Exception)
        {
            return false;
        }
    }

    private string BuildPublicUrl(string key) =>
        $"{settings.PublicBaseUrl.TrimEnd('/')}/{key}";

    private string? ExtractObjectKey(string publicUrl)
    {
        foreach (var baseUrl in GetAcceptedPublicBaseUrls())
        {
            if (!publicUrl.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
                continue;

            return publicUrl[baseUrl.Length..].TrimStart('/');
        }

        return null;
    }

    private IEnumerable<string> GetAcceptedPublicBaseUrls()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in new[]
                 {
                     settings.PublicBaseUrl,
                     BuildLegacyStorageBaseUrl()
                 })
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            var normalized = candidate.TrimEnd('/');
            if (seen.Add(normalized))
                yield return normalized;
        }
    }

    private string BuildLegacyStorageBaseUrl()
    {
        if (string.IsNullOrWhiteSpace(settings.BucketName) || string.IsNullOrWhiteSpace(settings.EndpointUrl))
            return string.Empty;

        if (!Uri.TryCreate(settings.EndpointUrl, UriKind.Absolute, out var endpoint))
            return string.Empty;

        return $"https://{settings.BucketName}.{endpoint.Host}";
    }

    private static string NormalizeContentType(string contentType, string fileName)
    {
        var normalized = (contentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();
        if (normalized is "image/jpg")
            return "image/jpeg";
        if (normalized is "audio/mp3")
            return "audio/mpeg";
        if (!string.IsNullOrWhiteSpace(normalized) && normalized != "application/octet-stream")
            return normalized;

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".mp3" => "audio/mpeg",
            ".m4a" or ".mp4" => "audio/mp4",
            ".aac" => "audio/aac",
            ".webm" => "audio/webm",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            _ => normalized
        };
    }

    private static void ValidateContentType(string contentType, string mediaType)
    {
        var valid = mediaType switch
        {
            "cover" or "icon" or "drawing" => ImageTypes.Contains(contentType),
            "audio" => AudioTypes.Contains(contentType),
            _ => false
        };

        if (!valid)
            throw new ArgumentException($"نوع فایل {contentType} برای {mediaType} مجاز نیست.");
    }

    private static string BuildObjectKey(string mediaType, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = mediaType == "audio" ? ".m4a" : ".jpg";
        var id = Guid.NewGuid().ToString("N");

        return mediaType switch
        {
            "cover" => $"covers/{id}{ext}",
            "audio" => $"audio/{id}{ext}",
            "icon" => $"icons/{id}{ext}",
            "drawing" => $"drawings/{id}{ext}",
            _ => $"misc/{id}{ext}"
        };
    }
}
