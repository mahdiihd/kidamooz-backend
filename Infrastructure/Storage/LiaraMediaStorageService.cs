using Amazon.S3;
using Amazon.S3.Model;
using Kidamooz.DTOs;

namespace Kidamooz.Infrastructure.Storage;

public interface IMediaStorageService
{
    UploadUrlResponseDto CreateUploadUrl(string fileName, string contentType, string mediaType);
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

        var publicUrl = $"{settings.PublicBaseUrl.TrimEnd('/')}/{key}";
        return new UploadUrlResponseDto(uploadUrl, publicUrl, new DateTimeOffset(expires, TimeSpan.Zero));
    }

    public bool ValidatePublicUrl(string publicUrl, string mediaType)
    {
        var baseUrl = settings.PublicBaseUrl.TrimEnd('/');
        if (!publicUrl.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            return false;

        var key = publicUrl[baseUrl.Length..].TrimStart('/');
        var prefix = mediaType switch
        {
            "cover" => "covers/",
            "audio" => "audio/",
            "icon" => "icons/",
            _ => null
        };

        return prefix != null && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
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
