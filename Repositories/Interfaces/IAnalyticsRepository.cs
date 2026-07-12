namespace Kidamooz.Repositories.Interfaces;

public interface IAnalyticsRepository
{
    Task RecordAppOpenAsync(CancellationToken ct = default);
}
