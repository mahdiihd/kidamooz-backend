namespace Kidamooz.Services;

public sealed class DailyStoryLimitException(DateTimeOffset nextAvailableAt)
    : InvalidOperationException("daily_limit_reached")
{
    public DateTimeOffset NextAvailableAt { get; } = nextAvailableAt;
}
