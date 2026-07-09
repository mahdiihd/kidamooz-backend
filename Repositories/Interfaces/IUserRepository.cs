using Kidamooz.Domain.Entities;

namespace Kidamooz.Repositories.Interfaces;

public interface IUserRepository
{
    Task<List<AdminUser>> GetAllAsync(CancellationToken ct = default);
    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task AddUserAsync(AdminUser user, CancellationToken ct = default);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string hash, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
