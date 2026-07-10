using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace Kidamooz.Services;

public interface IAuthService
{
    Task<AuthTokensDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<AuthTokensDto> RefreshAsync(RefreshRequestDto request, CancellationToken ct = default);
    Task LogoutAsync(RefreshRequestDto request, CancellationToken ct = default);
}

public class AuthService(
    IUserRepository userRepository,
    JwtTokenService jwtTokenService,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;
    public async Task<AuthTokensDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(email, ct);
        if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("ایمیل یا رمز عبور اشتباه است");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthTokensDto> RefreshAsync(RefreshRequestDto request, CancellationToken ct = default)
    {
        var hash = JwtTokenService.HashToken(request.RefreshToken);
        var stored = await userRepository.GetRefreshTokenByHashAsync(hash, ct);
        if (stored == null || stored.ExpiresAt < DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("توکن نامعتبر است");

        stored.RevokedAt = DateTimeOffset.UtcNow;
        await userRepository.SaveChangesAsync(ct);

        return await IssueTokensAsync(stored.User, ct);
    }

    public async Task LogoutAsync(RefreshRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return;

        var hash = JwtTokenService.HashToken(request.RefreshToken);
        var stored = await userRepository.GetRefreshTokenByHashAsync(hash, ct);
        if (stored != null)
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await userRepository.SaveChangesAsync(ct);
        }
    }

    private async Task<AuthTokensDto> IssueTokensAsync(AdminUser user, CancellationToken ct)
    {
        var refreshToken = JwtTokenService.GenerateRefreshToken();
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = JwtTokenService.HashToken(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await userRepository.AddRefreshTokenAsync(token, ct);
        return new AuthTokensDto(jwtTokenService.GenerateAccessToken(user), refreshToken);
    }
}
