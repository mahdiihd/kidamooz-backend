namespace Kidamooz.Repositories.Interfaces;

public interface IDashboardRepository
{
    Task<(int Total, int Today, int ThisWeek)> GetViewStatsAsync(CancellationToken ct = default);
}
