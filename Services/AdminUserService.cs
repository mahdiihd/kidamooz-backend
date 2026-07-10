using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.Repositories.Interfaces;

namespace Kidamooz.Services;

public interface IAdminUserService
{
    Task<List<AdminUserDto>> GetAllAsync(CancellationToken ct = default);
    Task<AdminUserDto> CreateAsync(CreateAdminUserRequestDto request, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid id, ResetAdminPasswordRequestDto request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class AdminUserService(
    IUserRepository userRepository,
    AppDbContext db,
    ICurrentUserService currentUser) : IAdminUserService
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

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("کاربر یافت نشد");

        if (currentUser.UserId == user.Id)
            throw new InvalidOperationException("نمی‌توانید حساب خودتان را حذف کنید.");

        if (user.Role == "admin")
        {
            var adminCount = await userRepository.CountAdminsAsync(ct);
            if (adminCount <= 1)
                throw new InvalidOperationException("حداقل یک ادمین باید باقی بماند.");
        }

        await userRepository.DeleteUserAsync(user, ct);
    }

    private static AdminUserDto ToDto(AdminUser user) => new(
        user.Id.ToString(),
        user.Email,
        user.DisplayName,
        user.Role,
        user.IsActive,
        user.CreatedAt);
}
