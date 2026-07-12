using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace Kidamooz.Infrastructure.Push;

public interface IPushNotificationSender
{
    Task<PushSendResult> SendAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken ct = default);
}

public record PushSendResult(int SuccessCount, int FailureCount, IReadOnlyList<string> InvalidTokens);

public class FirebasePushNotificationSender(
    FirebaseSettings settings,
    IHttpClientFactory httpClientFactory,
    ILogger<FirebasePushNotificationSender> logger) : IPushNotificationSender
{
    private static readonly string[] Scopes = ["https://www.googleapis.com/auth/firebase.messaging"];
    private string? cachedAccessToken;
    private DateTimeOffset accessTokenExpiresAt;

    public async Task<PushSendResult> SendAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken ct = default)
    {
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException("Firebase credentials are not configured.");
        }

        if (tokens.Count == 0)
        {
            return new PushSendResult(0, 0, []);
        }

        var accessToken = await GetAccessTokenAsync(ct);
        var client = httpClientFactory.CreateClient("firebase");
        var invalidTokens = new List<string>();
        var success = 0;
        var failure = 0;

        foreach (var batch in tokens.Chunk(500))
        {
            foreach (var token in batch)
            {
                var payload = new FcmMessageRequest(
                    new FcmMessage(
                        token,
                        new FcmNotification(title, body),
                        new FcmAndroidConfig(
                            "HIGH",
                            new FcmAndroidNotification("kidamooz_stories", "ic_stat_kidamooz", "#FFD166")),
                        data));

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://fcm.googleapis.com/v1/projects/{settings.ProjectId}/messages:send");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = JsonContent.Create(payload);

                using var response = await client.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    success++;
                    continue;
                }

                failure++;
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("FCM send failed for token ending {TokenSuffix}: {Status} {Body}",
                    token[^Math.Min(8, token.Length)..],
                    (int)response.StatusCode,
                    errorBody);

                if (errorBody.Contains("UNREGISTERED", StringComparison.OrdinalIgnoreCase) ||
                    errorBody.Contains("INVALID_ARGUMENT", StringComparison.OrdinalIgnoreCase) ||
                    errorBody.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase))
                {
                    invalidTokens.Add(token);
                }
            }
        }

        return new PushSendResult(success, failure, invalidTokens);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(cachedAccessToken) &&
            accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return cachedAccessToken;
        }

        var now = DateTimeOffset.UtcNow;
        var privateKey = settings.PrivateKey
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("-----BEGIN PRIVATE KEY-----", string.Empty, StringComparison.Ordinal)
            .Replace("-----END PRIVATE KEY-----", string.Empty, StringComparison.Ordinal)
            .Trim();

        using var rsa = RSA.Create();
        rsa.ImportFromPem($"-----BEGIN PRIVATE KEY-----\n{privateKey}\n-----END PRIVATE KEY-----");

        var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        var header = new JwtHeader(credentials);
        var payload = new JwtPayload
        {
            { "iss", settings.ClientEmail },
            { "scope", string.Join(' ', Scopes) },
            { "aud", "https://oauth2.googleapis.com/token" },
            { "iat", now.ToUnixTimeSeconds() },
            { "exp", now.AddHours(1).ToUnixTimeSeconds() },
        };

        var assertion = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));
        var client = httpClientFactory.CreateClient("firebase");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            ["assertion"] = assertion,
        });

        using var response = await client.PostAsync("https://oauth2.googleapis.com/token", content, ct);
        response.EnsureSuccessStatusCode();
        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Firebase OAuth token response was empty.");

        cachedAccessToken = tokenResponse.AccessToken;
        accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, tokenResponse.ExpiresIn - 60));
        return cachedAccessToken;
    }

    private sealed record GoogleTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed record FcmMessageRequest(FcmMessage Message);

    private sealed record FcmMessage(
        string Token,
        FcmNotification Notification,
        FcmAndroidConfig Android,
        IReadOnlyDictionary<string, string>? Data);

    private sealed record FcmNotification(string Title, string Body);

    private sealed record FcmAndroidConfig(
        string Priority,
        FcmAndroidNotification Notification);

    private sealed record FcmAndroidNotification(
        [property: JsonPropertyName("channel_id")] string ChannelId,
        string Icon,
        string Color);
}
