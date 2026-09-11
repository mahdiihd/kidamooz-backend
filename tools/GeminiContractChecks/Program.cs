using System.Net;
using System.Text;
using System.Text.Json;
using Kidamooz.Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Kidamooz.Controllers.Public;
using Kidamooz.Services;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// In-memory HTTP only: no credentials, network, application startup or database.
var settings = new GeminiSettings { ApiKey = "contract-test-placeholder" };
var handler = new CaptureHandler();
using var http = new HttpClient(handler);
var factory = new TestClientFactory(http);
var stories = new GeminiStoryClient(settings, factory, NullLogger<GeminiStoryClient>.Instance);
var covers = new GeminiCoverImageGenerator(settings, factory, NullLogger<GeminiCoverImageGenerator>.Instance);
var generated = new
{
    titleFa = "قصه", descriptionFa = "نقاشی کودک",
    storyScript = "یک روز زیبا بود.", coverPrompt = "A joyful garden"
};
handler.Reply = TextReply(JsonSerializer.Serialize(generated));
byte[] drawing = [1, 2, 3];
using var stream = new MemoryStream(drawing);
var story = await stories.GenerateFromDrawingAsync(stream, "image/png", "drawing.png");
CheckRequest(settings.Model);
using (var body = JsonDocument.Parse(handler.Body))
{
    var parts = body.RootElement.GetProperty("contents")[0].GetProperty("parts");
    Assert(parts[1].GetProperty("inline_data").GetProperty("data").GetString() == Convert.ToBase64String(drawing), "Drawing bytes");
    Assert(parts[1].GetProperty("inline_data").GetProperty("mime_type").GetString() == "image/png", "Drawing MIME");
    Assert(body.RootElement.GetProperty("generationConfig").GetProperty("temperature").GetDouble() == 0.8, "Story temperature");
    Assert(body.RootElement.GetProperty("generationConfig").GetProperty("responseMimeType").GetString() == "application/json", "JSON response");
}
Assert(story.CoverPrompt == generated.coverPrompt, "Parsed story output");
Assert(story.TitleEn == story.TitleFa, "Legacy fields use Persian fallback");

await stories.RewriteAsync("قصه", "نقاشی", "یک روز زیبا", "shorter");
CheckRequest(settings.Model);
var beforeEnglish = handler.Count;
var english = await stories.EnsureEnglishAsync("قصه", "نقاشی", null, null);
Assert(english.TitleEn == "قصه" && handler.Count == beforeEnglish, "No translation API request");

foreach (var field in new[] { "inlineData", "inline_data" })
{
    handler.Reply = "{\"candidates\":[{\"content\":{\"parts\":[{\"" + field + "\":{\"data\":\"AQID\",\"mimeType\":\"image/png\"}}]}}]}";
    var image = await covers.GenerateAsync("A joyful garden");
    CheckRequest(settings.CoverImageModel);
    Assert(image is not null && image.SequenceEqual(drawing), "Cover bytes");
    using var body = JsonDocument.Parse(handler.Body);
    var parts = body.RootElement.GetProperty("contents")[0].GetProperty("parts");
    Assert(parts.GetArrayLength() == 1 && parts[0].TryGetProperty("text", out _), "Text-only cover input");
    Assert(body.RootElement.GetProperty("generationConfig").GetProperty("responseModalities")[1].GetString() == "IMAGE", "Image response modality");
}
settings.BaseUrl = "https://1xai.ir/gemini/";
handler.Status = HttpStatusCode.BadGateway;
Assert(await covers.GenerateAsync("A garden") is null, "Cover failure fallback contract");
CheckRequest(settings.CoverImageModel);
settings.ApiKey = "";
var before = handler.Count;
Assert(await covers.GenerateAsync("A garden") is null && handler.Count == before, "Missing key prevents request");
var usageLogger = new UsageLogger();
using (var usage = JsonDocument.Parse("{\"usageMetadata\":{\"promptTokenCount\":100,\"candidatesTokenCount\":200,\"thoughtsTokenCount\":50,\"totalTokenCount\":350},\"text\":\"PRIVATE_CONTENT\"}"))
    GeminiUsage.Log(usageLogger, usage.RootElement, "story", settings.Model);
Assert(usageLogger.Last["TotalTokens"] is long total && total == 350, "Provider total preserved");
Assert(usageLogger.Last["ThinkingTokens"] is long thinking && thinking == 50, "Thinking counted separately");
Assert(!usageLogger.Message.Contains("PRIVATE_CONTENT"), "Response content excluded from logs");
using (var missing = JsonDocument.Parse("{}"))
    GeminiUsage.Log(usageLogger, missing.RootElement, "cover", settings.CoverImageModel);
Assert(usageLogger.Last["TotalTokens"] is null && usageLogger.Last["UsageAvailable"] is false, "Missing usage is not zero");
using (var invalid = JsonDocument.Parse("{\"usageMetadata\":{\"totalTokenCount\":\"invalid\"}}"))
    GeminiUsage.Log(usageLogger, invalid.RootElement, "cover", settings.CoverImageModel);
Assert(usageLogger.Last["TotalTokens"] is null, "Invalid usage does not break generation");
var draftService = DispatchProxy.Create<IStoryDraftService, DraftServiceCapture>();
var draftCapture = (DraftServiceCapture)draftService;
var controller = new StoryDraftsController(draftService, new TestMember())
{
    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
};
using var uploadStream = new MemoryStream(drawing);
var upload = new FormFile(uploadStream, 0, drawing.Length, "drawing", "drawing.png");
await controller.Create(upload, CancellationToken.None);
Assert(draftCapture.GenerateCover == false, "Old clients default to original drawing");
await controller.Create(upload, CancellationToken.None, true);
Assert(draftCapture.GenerateCover == true, "Explicit AI cover choice reaches service");
Console.WriteLine("PASS: Persian generation, usage logs and explicit cover consent; no network used.");

void CheckRequest(string model)
{
    Assert(handler.Uri == $"https://1xai.ir/gemini/v1beta/models/{model}:generateContent", "Native endpoint without query credentials");
    Assert(handler.Key == "contract-test-placeholder", "API key header");
    Assert(handler.Method == HttpMethod.Post && handler.MediaType == "application/json", "POST JSON");
}
static void Assert(bool condition, string label)
{
    if (!condition) throw new InvalidOperationException($"Contract failed: {label}");
}
static string TextReply(string text) => JsonSerializer.Serialize(new { candidates = new[] { new { content = new { parts = new[] { new { text } } } } } });

sealed class TestClientFactory(HttpClient client) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => client;
}
sealed class UsageLogger : ILogger
{
    public Dictionary<string, object?> Last { get; private set; } = [];
    public string Message { get; private set; } = "";
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel level) => true;
    public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter)
    {
        Last = ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(x => x.Key, x => x.Value);
        Message = formatter(state, error);
    }
}
sealed class TestMember : IMemberContext
{
    public string? UserId => "test-member";
    public bool IsMember => true;
}
public class DraftServiceCapture : DispatchProxy
{
    public bool? GenerateCover { get; private set; }
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method?.Name != nameof(IStoryDraftService.CreateFromDrawingAsync))
            throw new InvalidOperationException("Unexpected service call");
        GenerateCover = (bool)args![4]!;
        return Task.FromResult(JsonSerializer.Deserialize<StoryDraftDto>("{}")!);
    }
}
sealed class CaptureHandler : HttpMessageHandler
{
    public string Reply { get; set; } = "{}";
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public string? Uri { get; private set; }
    public string? Key { get; private set; }
    public string Body { get; private set; } = "";
    public HttpMethod? Method { get; private set; }
    public string? MediaType { get; private set; }
    public int Count { get; private set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Count++;
        Uri = request.RequestUri?.AbsoluteUri;
        Key = request.Headers.TryGetValues("x-goog-api-key", out var values) ? values.Single() : null;
        Body = await request.Content!.ReadAsStringAsync(ct);
        Method = request.Method;
        MediaType = request.Content.Headers.ContentType?.MediaType;
        return new HttpResponseMessage(Status) { Content = new StringContent(Reply, Encoding.UTF8, "application/json") };
    }
}
