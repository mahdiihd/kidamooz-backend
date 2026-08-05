namespace Kidamooz.Infrastructure.Cover;

public interface ICoverGenerationService
{
    Task<string?> GenerateAsync(Guid storyId, string prompt, CancellationToken ct = default);
}
