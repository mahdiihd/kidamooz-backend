namespace Kidamooz.Domain.Entities;

public static class MemberPlans
{
    public const string Free = "free";
    public const string Plus = "plus";

    public static int DailyCreateLimitFor(string? plan) =>
        string.Equals(plan, Plus, StringComparison.OrdinalIgnoreCase) ? 5 : 1;
}
