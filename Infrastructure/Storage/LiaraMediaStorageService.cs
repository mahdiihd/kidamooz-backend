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
    private static readonly HashSet<string> AudioTypes = ["audio/mpeg", "audio/mp4"];

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
        ValidateContentType(contentType, mediaType);

        var key = BuildObjectKey(mediaType, fileName);
        var request = new PutObjectRequest
        {
            BucketName = settings.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };

        await s3.PutObjectAsync(request, ct);
        return BuildPublicUrl(key);
    }

    public async Task<bool> ObjectExistsAsync(string publicUrl, CancellationToken ct = default)
    {
        var key = ExtractObjectKey(publicUrl);
        if (key is null)
            return false;

        try
        {
            await s3.GetObjectMetadataAsync(settings.BucketName, key, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
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
            _ => null
        };

        return prefix != null && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private string BuildPublicUrl(string key) =>
        $"{settings.PublicBaseUrl.TrimEnd('/')}/{key}";

    private string? ExtractObjectKey(string publicUrl)
    {
        var baseUrl = settings.PublicBaseUrl.TrimEnd('/');
        if (!publicUrl.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            return null;

        return publicUrl[baseUrl.Length..].TrimStart('/');
    }

    private static void ValidateContentType(string contentType, string mediaType)
    {
        var valid = mediaType switch
        {
            "cover" or "icon" => ImageTypes.Contains(contentType),
            "audio" => AudioTypes.Contains(contentType),
            _ => false
        };

        if (!valid)
            throw new ArgumentException($"نوع فایل {contentType} برای {mediaType} مجاز نیست.");
    }

    private static string BuildObjectKey(string mediaType, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        var id = Guid.NewGuid().ToString("N");

        return mediaType switch
        {
            "cover" => $"covers/{id}{ext}",
            "audio" => $"audio/{id}{ext}",
            "icon" => $"icons/{id}{ext}",
            _ => $"misc/{id}{ext}"
        };
    }
}
