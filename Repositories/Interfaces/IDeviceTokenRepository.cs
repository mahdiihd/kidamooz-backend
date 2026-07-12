using Kidamooz.Domain.Entities;

namespace Kidamooz.Repositories.Interfaces;

public interface IDeviceTokenRepository
{
    Task UpsertAsync(string token, string platform, string? appVersion, string? userId, CancellationToken ct = default);
    Task DeactivateAsync(string token, CancellationToken ct = default);
    Task DeactivateManyAsync(IEnumerable<string> tokens, CancellationToken ct = default);
    Task<List<DeviceToken>> GetActiveAsync(string platform, IReadOnlyCollection<string>? userIds = null, CancellationToken ct = default);
}
