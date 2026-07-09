using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<List<AdminUser>> GetAllAsync(CancellationToken ct = default) =>
        db.Users.OrderBy(u => u.Email).ToListAsync(ct);

    public Task<AdminUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Email == email, ct);

    public async Task AddUserAsync(AdminUser user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string hash, CancellationToken ct = default) =>
        db.RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
