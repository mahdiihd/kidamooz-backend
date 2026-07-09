using Kidamooz.Data;
using Kidamooz.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Repositories;

public class DashboardRepository(AppDbContext db) : IDashboardRepository
{
    public async Task<(int Total, int Today, int ThisWeek)> GetViewStatsAsync(CancellationToken ct = default)
    {
        var total = await db.StoryViewsDaily.SumAsync(v => (int?)v.ViewCount, ct) ?? 0;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekStart = today.AddDays(-6);

        var todayCount = await db.StoryViewsDaily
            .Where(v => v.ViewDate == today)
            .SumAsync(v => (int?)v.ViewCount, ct) ?? 0;

        var weekCount = await db.StoryViewsDaily
            .Where(v => v.ViewDate >= weekStart && v.ViewDate <= today)
            .SumAsync(v => (int?)v.ViewCount, ct) ?? 0;

        return (total, todayCount, weekCount);
    }
}
