namespace Kidamooz.Infrastructure;

public static class TehranTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone);

    public static DateTimeOffset StartOfTodayUtc()
    {
        var local = Now;
        var startLocal = new DateTimeOffset(local.Year, local.Month, local.Day, 0, 0, 0, local.Offset);
        return startLocal.ToUniversalTime();
    }

    public static DateTimeOffset StartOfTomorrowUtc() => StartOfTodayUtc().AddDays(1);

    private static TimeZoneInfo ResolveZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "Iran Standard Time" : "Asia/Tehran");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                "Asia/Tehran",
                TimeSpan.FromHours(3.5),
                "Iran Standard Time",
                "Iran Standard Time");
        }
    }
}
