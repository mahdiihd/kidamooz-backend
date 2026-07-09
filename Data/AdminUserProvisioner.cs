using Kidamooz.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Data;

public static class AdminUserProvisioner
{
    private static readonly HashSet<string> AllowedRoles = ["admin", "editor"];

    public static async Task CreateAsync(
        AppDbContext db,
        string email,
        string password,
        string displayName,
        string role = "editor",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("ایمیل الزامی است.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new ArgumentException("رمز عبور باید حداقل ۶ کاراکتر باشد.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("نام نمایشی الزامی است.");

        if (!AllowedRoles.Contains(role))
            throw new ArgumentException("نقش باید admin یا editor باشد.");

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail, ct))
            throw new InvalidOperationException($"کاربری با ایمیل {normalizedEmail} وجود دارد.");

        var now = DateTimeOffset.UtcNow;
        db.Users.Add(new AdminUser
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            DisplayName = displayName.Trim(),
            Role = role,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        await db.SaveChangesAsync(ct);
    }

    public static async Task ResetPasswordAsync(
        AppDbContext db,
        string email,
        string password,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new ArgumentException("رمز عبور باید حداقل ۶ کاراکتر باشد.");

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct)
            ?? throw new InvalidOperationException($"کاربری با ایمیل {normalizedEmail} یافت نشد.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
