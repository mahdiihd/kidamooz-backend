using System.Text.Json;
using System.Net;
using Kidamooz.Data;
using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:Secret"] = "test-only-otp-signing-secret-at-least-32-characters",
    ["SmsIr:ApiKey"] = "test-only-key", ["SmsIr:Mode"] = "verify",
    ["SmsIr:TemplateId"] = "123456",
    ["SmsIr:CodeParameter"] = "CODE"
}).Build();
var handler = new ProviderHandler();
var provider = new SmsIrOtpSender(new HttpClient(handler), configuration);
await provider.SendAsync("09120000000", "123456", default);
using (var payload = JsonDocument.Parse(handler.Body!))
{
    var root = payload.RootElement;
    Check(root.GetProperty("mobile").GetString() == "09120000000"
        && root.GetProperty("templateId").GetInt32() == 123456
        && root.GetProperty("parameters")[0].GetProperty("name").GetString() == "CODE"
        && root.GetProperty("parameters")[0].GetProperty("value").GetString() == "123456", "Sms.ir JSON contract");
}
Check(handler.ApiKey == "test-only-key" && handler.Url == "https://api.sms.ir/v1/send/verify"
    && handler.Method == HttpMethod.Post && handler.ContentType == "application/json", "Sms.ir header and endpoint contract");
handler.Status = HttpStatusCode.OK;
handler.Json = "{\"status\":104}";
await Reject<OtpDeliveryException>(() => provider.SendAsync("09120000000", "123456", default), "Provider semantic failure");
handler.Json = "not json";
await Reject<OtpDeliveryException>(() => provider.SendAsync("09120000000", "123456", default), "Malformed provider response");
handler.Status = HttpStatusCode.Unauthorized;
handler.Json = "{\"status\":1}";
await Reject<OtpDeliveryException>(() => provider.SendAsync("09120000000", "123456", default), "HTTP failure overrides successful body");
handler.Status = HttpStatusCode.OK;
handler.Json = "{\"status\":\"unexpected\"}";
await Reject<OtpDeliveryException>(() => provider.SendAsync("09120000000", "123456", default), "Invalid response schema rejected");
var missingTemplate = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["SmsIr:ApiKey"] = "test-only-key", ["SmsIr:Mode"] = "verify", ["SmsIr:CodeParameter"] = "CODE"
}).Build();
await Reject<OtpDeliveryException>(() => new SmsIrOtpSender(new HttpClient(handler), missingTemplate)
    .SendAsync("09120000000", "123456", default), "Missing template rejected");
handler.Throw = true;
await Reject<OtpDeliveryException>(() => provider.SendAsync("09120000000", "123456", default), "Provider transport failure redacted");

var bulkConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["SmsIr:ApiKey"] = "test-only-key", ["SmsIr:Mode"] = "bulk", ["SmsIr:LineNumber"] = "30000000000000"
}).Build();
var bulkHandler = new ProviderHandler { Json = "{\"status\":1,\"data\":{\"messageIds\":[123]}}" };
var bulkSender = new SmsIrOtpSender(new HttpClient(bulkHandler), bulkConfiguration);
await bulkSender.SendAsync("09120000000", "123456", default);
using (var payload = JsonDocument.Parse(bulkHandler.Body!))
{
    var root = payload.RootElement;
    Check(bulkHandler.Url == "https://api.sms.ir/v1/send/bulk"
        && root.GetProperty("lineNumber").GetInt64() == 30000000000000
        && root.GetProperty("mobiles")[0].GetString() == "09120000000"
        && root.GetProperty("messageText").GetString()!.Contains("123456")
        && root.GetProperty("sendDateTime").ValueKind == JsonValueKind.Null, "Bulk OTP contract without template");
}
bulkHandler.Json = "{\"status\":1,\"data\":{\"messageIds\":[null]}}";
await Reject<OtpDeliveryException>(() => bulkSender.SendAsync("09120000000", "123456", default), "Rejected recipient is not reported as sent");

if (!args.Contains("--local-db"))
{
    Console.WriteLine("Database checks skipped; pass --local-db from Back to use a disposable local SQL database.");
    return;
}

var local = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", optional: true).Build();
var connection = new SqlConnectionStringBuilder(local.GetConnectionString("Default"));
var host = connection.DataSource.ToLowerInvariant();
if (!(host == "." || host.StartsWith("localhost") || host.StartsWith("127.0.0.1") || host.StartsWith("(localdb)") || host.StartsWith(Environment.MachineName.ToLowerInvariant())))
    throw new InvalidOperationException("Database checks require a local SQL server.");
connection.InitialCatalog = "KidamoozAuthChecks_" + Guid.NewGuid().ToString("N");
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options;
await using var db = new AppDbContext(options);
try
{
    await db.Database.EnsureCreatedAsync();
    var sender = new FakeSender();
    var otp = new MemberOtpService(db, sender, configuration);
    var jwt = new JwtTokenService(Options.Create(new JwtSettings
    {
        Secret = configuration["Jwt:Secret"]!, Issuer = "test", Audience = "test"
    }));
    var auth = new MemberAuthService(db, jwt, otp);
    const string mobile = "09120000000";
    await Reject<ArgumentException>(() => otp.RequestAsync("123", default), "Invalid mobile rejected");
    await otp.RequestAsync(mobile, default);
    var firstCode = sender.Code!;
    await Reject<OtpRateLimitException>(() => otp.RequestAsync(mobile, default), "Resend cooldown");
    await Reject<UnauthorizedAccessException>(() => otp.VerifyAsync(mobile, "000000", default), "Wrong code rejected");
    var user = await auth.LoginOrRegisterAsync(new MemberAuthRequestDto(mobile, firstCode));
    Check(!user.User.ProfileComplete && user.AccessToken.Length > 0, "New user requires profile completion");
    await Reject<UnauthorizedAccessException>(() => otp.VerifyAsync(mobile, firstCode, default), "OTP replay rejected");
    await Reject<ArgumentException>(() => auth.UpdateProfileAsync(user.User.Id, new UpdateMemberProfileRequestDto(" ")), "Empty profile rejected");
    Check((await auth.UpdateProfileAsync(user.User.Id, new UpdateMemberProfileRequestDto("آزمایش"))).ProfileComplete, "Profile completion saved");
    var row = await db.MemberOtps.SingleAsync();
    row.LastSentAt = DateTimeOffset.UtcNow.AddMinutes(-2);
    await db.SaveChangesAsync();
    await otp.RequestAsync(mobile, default);
    var returning = await auth.LoginOrRegisterAsync(new MemberAuthRequestDto(mobile, sender.Code!));
    Check(returning.User.Id == user.User.Id && returning.User.ProfileComplete && await db.AppUsers.CountAsync() == 1, "Existing user signs in without duplicate registration");
    row.LastSentAt = DateTimeOffset.UtcNow.AddMinutes(-2);
    await db.SaveChangesAsync();
    await otp.RequestAsync(mobile, default);
    for (var i = 0; i < 5; i++) await Reject<UnauthorizedAccessException>(() => otp.VerifyAsync(mobile, "000000", default), "Failed attempt " + (i + 1));
    await Reject<UnauthorizedAccessException>(() => otp.VerifyAsync(mobile, sender.Code!, default), "Attempt cap rejects even correct code");
    row.LastSentAt = DateTimeOffset.UtcNow.AddMinutes(-2);
    await db.SaveChangesAsync();
    await otp.RequestAsync(mobile, default);
    row.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1);
    await db.SaveChangesAsync();
    await Reject<UnauthorizedAccessException>(() => otp.VerifyAsync(mobile, sender.Code!, default), "Expired code rejected");
    row.SendCount = 5;
    row.LastSentAt = DateTimeOffset.UtcNow.AddMinutes(-2);
    await db.SaveChangesAsync();
    await Reject<OtpRateLimitException>(() => otp.RequestAsync(mobile, default), "Hourly send cap");
    Console.WriteLine("All local database authentication checks passed.");
}
catch (Exception ex)
{
    Console.WriteLine("Database check failed: " + ex.GetType().Name + ". Connection details omitted.");
    Environment.ExitCode = 1;
}
finally
{
    try
    {
        if (connection.InitialCatalog.StartsWith("KidamoozAuthChecks_")) await db.Database.EnsureDeletedAsync();
    }
    catch { Console.WriteLine("Test database cleanup could not connect. Database: " + connection.InitialCatalog); }
}

static void Check(bool condition, string label)
{
    if (!condition) throw new Exception(label);
    Console.WriteLine("PASS: " + label);
}

static async Task Reject<T>(Func<Task> action, string label) where T : Exception
{
    try { await action(); }
    catch (T ex)
    {
        Check(!ex.Message.Contains("test-only-key"), label);
        return;
    }
    throw new Exception("Expected rejection: " + label);
}

sealed class FakeSender : IMemberOtpSender
{
    public string? Code { get; private set; }
    public Task SendAsync(string mobile, string code, CancellationToken ct) { Code = code; return Task.CompletedTask; }
}

sealed class ProviderHandler : HttpMessageHandler
{
    public string? Body { get; private set; }
    public string? ApiKey, Url, ContentType;
    public HttpMethod? Method;
    public HttpStatusCode Status = HttpStatusCode.OK;
    public string Json = "{\"status\":1,\"data\":{\"messageId\":1,\"cost\":1}}";
    public bool Throw;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (Throw) throw new HttpRequestException("test-only-key");
        ApiKey = request.Headers.GetValues("x-api-key").Single();
        Url = request.RequestUri?.ToString();
        Method = request.Method;
        ContentType = request.Content?.Headers.ContentType?.MediaType;
        Body = await request.Content!.ReadAsStringAsync(ct);
        return new HttpResponseMessage(Status) { Content = new StringContent(Json) };
    }
}
