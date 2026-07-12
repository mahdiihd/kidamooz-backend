using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Repositories;

public class AnalyticsRepository(AppDbContext db) : IAnalyticsRepository
{
    public async Task RecordAppOpenAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var row = await db.AppOpensDaily.FirstOrDefaultAsync(x => x.ViewDate == today, ct);
        if (row == null)
        {
            db.AppOpensDaily.Add(new AppOpensDaily
            {
                ViewDate = today,
                OpenCount = 1,
            });
        }
        else
        {
            row.OpenCount += 1;
        }

        await db.SaveChangesAsync(ct);
    }
}
