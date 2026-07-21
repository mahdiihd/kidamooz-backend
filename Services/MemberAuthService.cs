using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Services;

public interface IMemberAuthService
{
    Task<MemberAuthResponseDto> LoginOrRegisterAsync(MemberAuthRequestDto request, CancellationToken ct = default);
    Task<MemberProfileDto> GetProfileAsync(string userId, CancellationToken ct = default);
    Task<MemberProfileDto> UpdateProfileAsync(string userId, UpdateMemberProfileRequestDto request, CancellationToken ct = default);
}

public class MemberAuthService(AppDbContext db, JwtTokenService jwt) : IMemberAuthService
{
    public async Task<MemberAuthResponseDto> LoginOrRegisterAsync(
        MemberAuthRequestDto request,
        CancellationToken ct = default)
    {
        var mobile = MobileNormalizer.Normalize(request.Mobile);
        if (!MobileNormalizer.IsValidIranMobile(mobile))
            throw new ArgumentException("شماره موبایل معتبر نیست.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Trim().Length < 4)
            throw new ArgumentException("رمز عبور حداقل ۴ کاراکتر باشد.");

        var password = request.Password.Trim();
        var existing = await db.AppUsers.FirstOrDefaultAsync(x => x.Mobile == mobile, ct);
        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(existing.PasswordHash)
                || !BCrypt.Net.BCrypt.Verify(password, existing.PasswordHash))
                throw new UnauthorizedAccessException("شماره موبایل یا رمز عبور اشتباه است.");
            if (!existing.IsActive)
                throw new UnauthorizedAccessException("حساب کاربری غیرفعال است.");

            if (!string.IsNullOrWhiteSpace(request.DisplayName)
                && string.IsNullOrWhiteSpace(existing.DisplayName))
            {
                existing.DisplayName = PlainTextSanitizer.Clean(request.DisplayName, 200);
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }

            return Issue(existing);
        }

        var now = DateTimeOffset.UtcNow;
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? $"کاربر {mobile[^4..]}"
            : PlainTextSanitizer.Clean(request.DisplayName, 200);
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = $"کاربر {mobile[^4..]}";

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Mobile = mobile,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            DisplayName = displayName,
            Email = $"{mobile}@member.local",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.AppUsers.Add(user);
        await db.SaveChangesAsync(ct);
        return Issue(user);
    }

    public async Task<MemberProfileDto> GetProfileAsync(string userId, CancellationToken ct = default)
    {
        var user = await GetMemberAsync(userId, ct);
        return ToProfile(user);
    }

    public async Task<MemberProfileDto> UpdateProfileAsync(
        string userId,
        UpdateMemberProfileRequestDto request,
        CancellationToken ct = default)
    {
        var user = await GetMemberAsync(userId, ct);
        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            var cleaned = PlainTextSanitizer.Clean(request.DisplayName, 200);
            if (!string.IsNullOrWhiteSpace(cleaned))
                user.DisplayName = cleaned;
        }
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToProfile(user);
    }

    private async Task<AppUser> GetMemberAsync(string userId, CancellationToken ct)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new KeyNotFoundException("کاربر یافت نشد.");
        if (string.IsNullOrWhiteSpace(user.Mobile) || string.IsNullOrWhiteSpace(user.PasswordHash))
            throw new UnauthorizedAccessException("این حساب برای ورود اپ تنظیم نشده است.");
        return user;
    }

    private MemberAuthResponseDto Issue(AppUser user) =>
        new(jwt.GenerateMemberAccessToken(user), ToProfile(user));

    private static MemberProfileDto ToProfile(AppUser user) =>
        new(user.Id, user.Mobile ?? string.Empty, PlainTextSanitizer.Clean(user.DisplayName, 200));
}
