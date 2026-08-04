using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Services;

public interface IAdminMemberService
{
    Task<List<AppMemberAdminDto>> ListAsync(string? search, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

public class AdminMemberService(AppDbContext db) : IAdminMemberService
{
    public async Task<List<AppMemberAdminDto>> ListAsync(string? search, CancellationToken ct = default)
    {
        var query = db.AppUsers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.DisplayName.Contains(term) ||
                u.Email.Contains(term) ||
                (u.Mobile != null && u.Mobile.Contains(term)) ||
                u.Id.Contains(term));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(ct);

        return users.Select(ToDto).ToList();
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("کاربر یافت نشد");

        var audienceLinks = await db.StoryAudienceUsers.Where(x => x.UserId == id).ToListAsync(ct);
        db.StoryAudienceUsers.RemoveRange(audienceLinks);

        var devices = await db.DeviceTokens.Where(x => x.UserId == id).ToListAsync(ct);
        db.DeviceTokens.RemoveRange(devices);

        db.AppUsers.Remove(user);
        await db.SaveChangesAsync(ct);
    }

    private static AppMemberAdminDto ToDto(AppUser user) => new(
        user.Id,
        user.DisplayName,
        user.Mobile,
        user.Email,
        user.PlanTier,
        user.PlusExpiresAt,
        user.ListenStreak,
        user.CreateStreak,
        user.IsActive,
        user.CreatedAt,
        user.UpdatedAt);
}
