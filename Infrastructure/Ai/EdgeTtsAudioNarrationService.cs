using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;

namespace Kidamooz.Infrastructure.Ai;

public class EdgeTtsAudioNarrationService(
    IWebHostEnvironment environment,
    IOptions<NarrationSettings> options,
    ILogger<EdgeTtsAudioNarrationService> logger) : IAudioNarrationService
{
    private readonly NarrationSettings _settings = options.Value;

    public async Task<string?> GenerateAsync(
        Guid storyId,
        string storyText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storyText))
        {
            logger.LogWarning("Skipping narration for {StoryId}: empty story text", storyId);
            return null;
        }

        var outputRoot = ResolveOutputRoot();
        Directory.CreateDirectory(outputRoot);
        Directory.CreateDirectory(ResolveUserOutputRoot());

        var outputFileName = $"{storyId:N}.mp3";
        var outputPath = Path.Combine(outputRoot, outputFileName);
        var tempTextPath = Path.Combine(Path.GetTempPath(), $"kidamooz-tts-{storyId:N}.txt");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await File.WriteAllTextAsync(tempTextPath, storyText.Trim(), Encoding.UTF8, cancellationToken);

            var startInfo = new ProcessStartInfo
            {
                FileName = _settings.Executable,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            startInfo.ArgumentList.Add("--voice");
            startInfo.ArgumentList.Add(_settings.Voice);
            startInfo.ArgumentList.Add("--file");
            startInfo.ArgumentList.Add(tempTextPath);
            startInfo.ArgumentList.Add("--write-media");
            startInfo.ArgumentList.Add(outputPath);

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            if (!process.Start())
            {
                logger.LogError("Failed to start edge-tts for {StoryId}", storyId);
                return null;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            stopwatch.Stop();

            if (process.ExitCode != 0 || !File.Exists(outputPath) || new FileInfo(outputPath).Length <= 0)
            {
                logger.LogError(
                    "edge-tts failed for {StoryId}. ExitCode={ExitCode}, DurationMs={DurationMs}, OutputPath={OutputPath}, StdOut={StdOut}, StdErr={StdErr}",
                    storyId,
                    process.ExitCode,
                    stopwatch.ElapsedMilliseconds,
                    outputPath,
                    TrimLog(stdout),
                    TrimLog(stderr));
                TryDelete(outputPath);
                return null;
            }

            var relativeUrl = $"/audio/{outputFileName}";
            logger.LogInformation(
                "Narration generated for {StoryId} in {DurationMs}ms at {OutputPath}",
                storyId,
                stopwatch.ElapsedMilliseconds,
                outputPath);
            return relativeUrl;
        }
        catch (OperationCanceledException)
        {
            TryDelete(outputPath);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "Narration generation threw for {StoryId} after {DurationMs}ms. OutputPath={OutputPath}",
                storyId,
                stopwatch.ElapsedMilliseconds,
                outputPath);
            TryDelete(outputPath);
            return null;
        }
        finally
        {
            TryDelete(tempTextPath);
        }
    }

    public string ResolveLocalPath(string relativeAudioUrl)
    {
        var fileName = Path.GetFileName(relativeAudioUrl);
        return Path.Combine(ResolveOutputRoot(), fileName);
    }

    private string ResolveOutputRoot() =>
        Path.GetFullPath(Path.Combine(environment.ContentRootPath, _settings.OutputFolder));

    private string ResolveUserOutputRoot() =>
        Path.GetFullPath(Path.Combine(environment.ContentRootPath, _settings.UserOutputFolder));

    private static string TrimLog(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var trimmed = value.Trim();
        return trimmed.Length <= 2000 ? trimmed : trimmed[..2000];
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignored
        }
    }
}
