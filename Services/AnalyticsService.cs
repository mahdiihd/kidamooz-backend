using Kidamooz.Repositories.Interfaces;

namespace Kidamooz.Services;

public interface IAnalyticsService
{
    Task RecordAppOpenAsync(CancellationToken ct = default);
}

public class AnalyticsService(IAnalyticsRepository analyticsRepository) : IAnalyticsService
{
    public Task RecordAppOpenAsync(CancellationToken ct = default) =>
        analyticsRepository.RecordAppOpenAsync(ct);
}
