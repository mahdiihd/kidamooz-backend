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

public class MemberAuthService(AppDbContext db, JwtTokenService jwt, MemberOtpService otp) : IMemberAuthService
{
    public async Task<MemberAuthResponseDto> LoginOrRegisterAsync(
        MemberAuthRequestDto request,
        CancellationToken ct = default)
    {
        var mobile = MemberOtpService.Normalize(request.Mobile);
        await otp.VerifyAsync(mobile, request.Code, ct);
        var existing = await db.AppUsers.FirstOrDefaultAsync(x => x.Mobile == mobile, ct);
        if (existing is not null)
        {
            if (!existing.IsActive)
                throw new UnauthorizedAccessException("حساب کاربری غیرفعال است.");

            return Issue(existing);
        }

        var now = DateTimeOffset.UtcNow;
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Mobile = mobile,
            DisplayName = string.Empty,
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
        var name = PlainTextSanitizer.Clean(request.DisplayName, 200);
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("نام را وارد کنید.");
        user.DisplayName = name;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToProfile(user);
    }

    private async Task<AppUser> GetMemberAsync(string userId, CancellationToken ct)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new KeyNotFoundException("کاربر یافت نشد.");
        if (string.IsNullOrWhiteSpace(user.Mobile) || !user.IsActive)
            throw new UnauthorizedAccessException("این حساب برای ورود اپ تنظیم نشده است.");
        return user;
    }

    private MemberAuthResponseDto Issue(AppUser user) =>
        new(jwt.GenerateMemberAccessToken(user), ToProfile(user));

    private static MemberProfileDto ToProfile(AppUser user) =>
        new(user.Id, user.Mobile ?? string.Empty, PlainTextSanitizer.Clean(user.DisplayName, 200), !string.IsNullOrWhiteSpace(user.DisplayName));
}
