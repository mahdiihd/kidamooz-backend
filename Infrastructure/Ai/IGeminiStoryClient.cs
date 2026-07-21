namespace Kidamooz.Infrastructure.Ai;

public interface IGeminiStoryClient
{
    Task<GeneratedStoryContent> GenerateFromDrawingAsync(
        Stream image,
        string contentType,
        string fileName,
        CancellationToken ct = default);
}
