using System.Data;
using System.Security.Cryptography;
using System.Text;
using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Services;

public sealed class OtpRateLimitException : Exception
{
    public OtpRateLimitException() : base("تعداد درخواست‌ها زیاد است. کمی بعد دوباره تلاش کنید.") { }
}

public sealed class OtpDeliveryException(string message) : InvalidOperationException(message);

public interface IMemberOtpSender
{
    Task SendAsync(string mobile, string code, CancellationToken ct);
}

public sealed class MemberOtpService(AppDbContext db, IMemberOtpSender sender, IConfiguration config)
{
    public static string Normalize(string mobile)
    {
        var digits = string.Concat((mobile ?? string.Empty).Select(c => c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') : c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') : c));
        var normalized = MobileNormalizer.Normalize(digits);
        if (!MobileNormalizer.IsValidIranMobile(normalized))
            throw new ArgumentException("شماره موبایل معتبر نیست.");
        return normalized;
    }

    public async Task RequestAsync(string mobile, CancellationToken ct)
    {
        mobile = Normalize(mobile);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = DateTimeOffset.UtcNow;
        var otp = await db.MemberOtps.SingleOrDefaultAsync(x => x.Mobile == mobile, ct);
        if (otp is null)
        {
            otp = new MemberOtp { Mobile = mobile, WindowStartedAt = now };
            db.MemberOtps.Add(otp);
        }
        if (now - otp.LastSentAt < TimeSpan.FromSeconds(60)) throw new OtpRateLimitException();
        if (now - otp.WindowStartedAt >= TimeSpan.FromHours(1))
        {
            otp.WindowStartedAt = now;
            otp.SendCount = 0;
        }
        if (otp.SendCount >= 5) throw new OtpRateLimitException();
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        otp.CodeHash = Hash(mobile, code);
        otp.ExpiresAt = now.AddMinutes(2);
        otp.LastSentAt = now;
        otp.SendCount++;
        otp.Attempts = 0;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        // Reserve limits before contacting the provider, including when delivery fails.
        await sender.SendAsync(mobile, code, ct);
    }

    public async Task VerifyAsync(string mobile, string code, CancellationToken ct)
    {
        mobile = Normalize(mobile);
        if (code is null || code.Length != 6 || code.Any(c => c < '0' || c > '9'))
            throw new UnauthorizedAccessException("کد نامعتبر یا منقضی شده است.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var otp = await db.MemberOtps.SingleOrDefaultAsync(x => x.Mobile == mobile, ct);
        if (otp is null || otp.ExpiresAt <= DateTimeOffset.UtcNow || otp.Attempts >= 5 || otp.CodeHash.Length == 0)
            throw new UnauthorizedAccessException("کد نامعتبر یا منقضی شده است.");
        otp.Attempts++;
        var valid = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(otp.CodeHash), Encoding.UTF8.GetBytes(Hash(mobile, code)));
        if (valid) otp.CodeHash = string.Empty;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        if (!valid) throw new UnauthorizedAccessException("کد نامعتبر یا منقضی شده است.");
    }

    private string Hash(string mobile, string code)
    {
        var secret = config["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("تنظیمات ورود ناقص است.");
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"member-otp:{mobile}:{code}")));
    }
}
