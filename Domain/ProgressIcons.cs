namespace Kidamooz.Domain;

public static class ProgressIcons
{
    public const string Default = "star";

    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "star",
        "boy",
        "forest",
        "magic",
        "panda",
        "rabbit",
        "rocket",
        "wolf",
        "parry",
    };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Default;

        var key = value.Trim().ToLowerInvariant();
        return Allowed.Contains(key) ? key : Default;
    }
}
