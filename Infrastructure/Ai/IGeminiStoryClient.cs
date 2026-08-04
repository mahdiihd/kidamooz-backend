namespace Kidamooz.Infrastructure.Ai;

public interface IGeminiStoryClient
{
    Task<GeneratedStoryContent> GenerateFromDrawingAsync(
        Stream image,
        string contentType,
        string fileName,
        CancellationToken ct = default);

    Task<GeneratedStoryContent> RewriteAsync(
        string titleFa,
        string descriptionFa,
        string storyScript,
        string mode,
        CancellationToken ct = default);

    Task<(string TitleEn, string DescriptionEn)> EnsureEnglishAsync(
        string titleFa,
        string descriptionFa,
        string? titleEn,
        string? descriptionEn,
        CancellationToken ct = default);
}
