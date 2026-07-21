using Kidamooz.DTOs;

namespace Kidamooz.Infrastructure.Storage;

public interface IMediaUrlNormalizer
{
    string Normalize(string? url);
    CategoryDto Normalize(CategoryDto dto);
    StoryDto Normalize(StoryDto dto);
    StoryDetailDto Normalize(StoryDetailDto dto);
    PublicStoryDto Normalize(PublicStoryDto dto);
    PublicStoryDetailDto Normalize(PublicStoryDetailDto dto);
}

public class MediaUrlNormalizer(LiaraSettings settings) : IMediaUrlNormalizer
{
    public string Normalize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var trimmed = url.Trim();
        if (!IsSafeMediaUrl(trimmed))
            return string.Empty;

        var publicBase = settings.PublicBaseUrl.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(publicBase))
            return trimmed;

        foreach (var legacyBase in GetLegacyBases())
        {
            if (!trimmed.StartsWith(legacyBase, StringComparison.OrdinalIgnoreCase))
                continue;

            return publicBase + trimmed[legacyBase.Length..];
        }

        return trimmed;
    }

    public static bool IsSafeMediaUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();
        if (trimmed.StartsWith('/') && !trimmed.StartsWith("//", StringComparison.Ordinal))
            return !trimmed.Contains('\0') && !trimmed.Contains('<');

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme is "http" or "https"
            && string.IsNullOrEmpty(uri.UserInfo)
            && !trimmed.Contains('<')
            && !trimmed.Contains('\0');
    }

    public CategoryDto Normalize(CategoryDto dto) =>
        dto with { IconUrl = Normalize(dto.IconUrl) };

    public StoryDto Normalize(StoryDto dto) =>
        dto with
        {
            CoverUrl = Normalize(dto.CoverUrl),
            AudioUrl = Normalize(dto.AudioUrl)
        };

    public StoryDetailDto Normalize(StoryDetailDto dto) =>
        dto with
        {
            CoverUrl = Normalize(dto.CoverUrl),
            AudioUrl = Normalize(dto.AudioUrl),
            Chapters = dto.Chapters?
                .Select(c => c with { ImageUrl = Normalize(c.ImageUrl) })
                .ToList()
        };

    public PublicStoryDto Normalize(PublicStoryDto dto) =>
        dto with
        {
            CoverUrl = Normalize(dto.CoverUrl),
            AudioUrl = Normalize(dto.AudioUrl)
        };

    public PublicStoryDetailDto Normalize(PublicStoryDetailDto dto) =>
        dto with
        {
            CoverUrl = Normalize(dto.CoverUrl),
            AudioUrl = Normalize(dto.AudioUrl),
            Chapters = dto.Chapters?
                .Select(c => c with { ImageUrl = Normalize(c.ImageUrl) })
                .ToList()
        };

    private IEnumerable<string> GetLegacyBases()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var publicBase = settings.PublicBaseUrl.TrimEnd('/');

        void Add(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var normalized = value.TrimEnd('/');
            if (string.Equals(normalized, publicBase, StringComparison.OrdinalIgnoreCase))
                return;

            seen.Add(normalized);
        }

        if (!string.IsNullOrWhiteSpace(settings.BucketName) && !string.IsNullOrWhiteSpace(settings.EndpointUrl)
            && Uri.TryCreate(settings.EndpointUrl, UriKind.Absolute, out var endpoint))
        {
            Add($"https://{settings.BucketName}.{endpoint.Host}");
            Add($"http://{settings.BucketName}.{endpoint.Host}");
        }

        Add("https://kid.storage.c2.liara.site");
        Add("http://kid.storage.c2.liara.site");
        Add("http://kidingo.ir");

        return seen;
    }
}
