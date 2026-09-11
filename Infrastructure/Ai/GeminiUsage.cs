using System.Diagnostics;
using System.Text.Json;

namespace Kidamooz.Infrastructure.Ai;

public static class GeminiUsage
{
    public static void Log(ILogger logger, JsonElement response, string operation, string model)
    {
        var present = response.TryGetProperty("usageMetadata", out var usage)
            && usage.ValueKind == JsonValueKind.Object;
        long? Read(string name) => present && usage.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt64(out var count) ? count : null;

        // Use only allowlisted counters. Never log prompts, responses or credentials.
        logger.LogInformation(
            "GeminiUsage Operation={Operation} Model={Model} TraceId={TraceId} UsageAvailable={UsageAvailable} PromptTokens={PromptTokens} OutputTokens={OutputTokens} ThinkingTokens={ThinkingTokens} CachedTokens={CachedTokens} TotalTokens={TotalTokens}",
            operation, model, Activity.Current?.TraceId.ToString(), present,
            Read("promptTokenCount"), Read("candidatesTokenCount"), Read("thoughtsTokenCount"),
            Read("cachedContentTokenCount"), Read("totalTokenCount"));
    }
}
