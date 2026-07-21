using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Kidamooz.Infrastructure.Security;

public static partial class PlainTextSanitizer
{
    public static string Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = value.Replace("\0", string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = DangerousBlocks().Replace(text, string.Empty);
        text = StripTags().Replace(text, string.Empty);
        text = DangerousProtocol().Replace(text, string.Empty);
        text = EventHandlerAttr().Replace(text, string.Empty);
        text = NormalizeWhitespace(text);

        if (text.Length > maxLength)
            text = text[..maxLength];

        return text.Trim();
    }

    public static string? CleanOptional(string? value, int maxLength)
    {
        var cleaned = Clean(value, maxLength);
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static string NormalizeWhitespace(string text)
    {
        var builder = new StringBuilder(text.Length);
        var previousWasNewline = false;

        foreach (var ch in text)
        {
            if (ch is '\r')
                continue;

            if (ch is '\n')
            {
                if (!previousWasNewline)
                    builder.Append('\n');
                previousWasNewline = true;
                continue;
            }

            previousWasNewline = false;
            if (char.IsControl(ch) && !char.IsWhiteSpace(ch))
                continue;

            builder.Append(ch is '\t' ? ' ' : ch);
        }

        return CollapseSpaces().Replace(builder.ToString(), " ");
    }

    [GeneratedRegex(
        @"<(script|style|iframe|object|embed|link|meta|svg|math)\b[^>]*>[\s\S]*?</\1\s*>|<(script|style|iframe|object|embed|link|meta|svg|math|img|video|audio|source|base)\b[^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DangerousBlocks();

    [GeneratedRegex("<[^>]*>", RegexOptions.Singleline)]
    private static partial Regex StripTags();

    [GeneratedRegex(
        @"\b(?:javascript|vbscript|data)\s*:",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DangerousProtocol();

    [GeneratedRegex(
        @"\bon[a-z]+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EventHandlerAttr();

    [GeneratedRegex(@"[^\S\n]+")]
    private static partial Regex CollapseSpaces();
}
