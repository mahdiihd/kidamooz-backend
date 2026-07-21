using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Kidamooz.Infrastructure.Auth;

public interface IMemberContext
{
    string? UserId { get; }
    bool IsMember { get; }
}

public class MemberContext(IHttpContextAccessor httpContextAccessor) : IMemberContext
{
    public string? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user is null || !user.IsInRole("member"))
                return null;
            return user.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }

    public bool IsMember => !string.IsNullOrWhiteSpace(UserId);
}

public static partial class MobileNormalizer
{
    public static string Normalize(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
            return string.Empty;

        var digits = DigitsOnly().Replace(mobile, string.Empty);
        if (digits.StartsWith("98") && digits.Length == 12)
            digits = "0" + digits[2..];
        if (digits.StartsWith("9") && digits.Length == 10)
            digits = "0" + digits;

        return digits;
    }

    public static bool IsValidIranMobile(string mobile) =>
        IranMobile().IsMatch(mobile);

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnly();

    [GeneratedRegex(@"^09\d{9}$")]
    private static partial Regex IranMobile();
}
