namespace Kidamooz.Infrastructure.Ai;

public interface ICoverImageGenerator
{
    Task<byte[]?> GenerateAsync(string coverPrompt, CancellationToken ct = default);
}
