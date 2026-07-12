using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Push;
using Kidamooz.Repositories.Interfaces;

namespace Kidamooz.Services;

public interface IDeviceService
{
    Task RegisterAsync(DeviceRegisterRequestDto request, CancellationToken ct = default);
    Task UnregisterAsync(DeviceUnregisterRequestDto request, CancellationToken ct = default);
}

public class DeviceService(IDeviceTokenRepository deviceTokens) : IDeviceService
{
    public Task RegisterAsync(DeviceRegisterRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new ArgumentException("Token is required.");
        }

        var platform = string.IsNullOrWhiteSpace(request.Platform)
            ? "android"
            : request.Platform.Trim().ToLowerInvariant();

        return deviceTokens.UpsertAsync(
            request.Token.Trim(),
            platform,
            request.AppVersion,
            request.UserId,
            ct);
    }

    public Task UnregisterAsync(DeviceUnregisterRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new ArgumentException("Token is required.");
        }

        return deviceTokens.DeactivateAsync(request.Token.Trim(), ct);
    }
}

public interface INotificationService
{
    Task<BroadcastNotificationResponseDto> BroadcastAsync(
        BroadcastNotificationRequestDto request,
        CancellationToken ct = default);
}

public class NotificationService(
    IDeviceTokenRepository deviceTokens,
    IPushNotificationSender pushSender) : INotificationService
{
    public async Task<BroadcastNotificationResponseDto> BroadcastAsync(
        BroadcastNotificationRequestDto request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            throw new ArgumentException("Title and body are required.");
        }

        var audience = string.IsNullOrWhiteSpace(request.Audience)
            ? "all"
            : request.Audience.Trim().ToLowerInvariant();

        IReadOnlyCollection<string>? userIds = null;
        if (audience == "users")
        {
            if (request.UserIds is not { Count: > 0 })
            {
                throw new ArgumentException("userIds is required when audience is users.");
            }

            userIds = request.UserIds;
        }
        else if (audience != "all")
        {
            throw new ArgumentException("Unsupported audience. Use all or users.");
        }

        var devices = await deviceTokens.GetActiveAsync("android", userIds, ct);
        var tokens = devices.Select(x => x.Token).Distinct().ToList();
        if (tokens.Count == 0)
        {
            return new BroadcastNotificationResponseDto(0, 0, 0);
        }

        var data = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(request.Data?.StoryId))
        {
            data["storyId"] = request.Data.StoryId!;
        }

        var result = await pushSender.SendAsync(tokens, request.Title.Trim(), request.Body.Trim(), data, ct);
        if (result.InvalidTokens.Count > 0)
        {
            await deviceTokens.DeactivateManyAsync(result.InvalidTokens, ct);
        }

        return new BroadcastNotificationResponseDto(tokens.Count, result.SuccessCount, result.FailureCount);
    }
}
