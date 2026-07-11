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
            return url ?? string.Empty;

        var publicBase = settings.PublicBaseUrl.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(publicBase))
            return url;

        foreach (var legacyBase in GetLegacyBases())
        {
            if (!url.StartsWith(legacyBase, StringComparison.OrdinalIgnoreCase))
                continue;

            return publicBase + url[legacyBase.Length..];
        }

        return url;
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
