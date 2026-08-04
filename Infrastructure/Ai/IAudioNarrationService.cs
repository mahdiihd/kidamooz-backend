namespace Kidamooz.Infrastructure.Ai;

public interface IAudioNarrationService
{
    Task<string?> GenerateAsync(
        Guid storyId,
        string storyText,
        CancellationToken cancellationToken = default);

    string ResolveLocalPath(string relativeAudioUrl);
}
