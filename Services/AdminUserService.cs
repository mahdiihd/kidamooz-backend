using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Kidamooz.Repositories.Interfaces;

namespace Kidamooz.Services;

public interface IAdminUserService
{
    Task<List<AdminUserDto>> GetAllAsync(CancellationToken ct = default);
    Task<AdminUserDto> CreateAsync(CreateAdminUserRequestDto request, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid id, ResetAdminPasswordRequestDto request, CancellationToken ct = default);
}

public class AdminUserService(IUserRepository userRepository, AppDbContext db) : IAdminUserService
{
    public async Task<List<AdminUserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await userRepository.GetAllAsync(ct);
        return users.Select(ToDto).ToList();
    }

    public async Task<AdminUserDto> CreateAsync(CreateAdminUserRequestDto request, CancellationToken ct = default)
    {
        await AdminUserProvisioner.CreateAsync(
            db,
            request.Email,
            request.Password,
            request.DisplayName,
            request.Role,
            ct);

        var user = await userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct)
            ?? throw new InvalidOperationException("کاربر ساخته نشد");

        return ToDto(user);
    }

    public async Task ResetPasswordAsync(Guid id, ResetAdminPasswordRequestDto request, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("کاربر یافت نشد");

        await AdminUserProvisioner.ResetPasswordAsync(db, user.Email, request.Password, ct);
    }

    private static AdminUserDto ToDto(AdminUser user) => new(
        user.Id.ToString(),
        user.Email,
        user.DisplayName,
        user.Role,
        user.IsActive,
        user.CreatedAt);
}
