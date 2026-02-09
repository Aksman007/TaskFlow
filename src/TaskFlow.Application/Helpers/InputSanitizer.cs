using System.Text.RegularExpressions;

namespace TaskFlow.Application.Helpers;

public static partial class InputSanitizer
{
    /// <summary>
    /// Strips HTML tags and dangerous content from user input to prevent XSS.
    /// </summary>
    public static string SanitizeHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove script tags and their content
        var result = ScriptTagRegex().Replace(input, string.Empty);

        // Remove style tags and their content
        result = StyleTagRegex().Replace(result, string.Empty);

        // Remove all HTML tags
        result = HtmlTagRegex().Replace(result, string.Empty);

        // Remove event handlers (onclick, onerror, etc.)
        result = EventHandlerRegex().Replace(result, string.Empty);

        // Remove javascript: and data: protocols
        result = ProtocolRegex().Replace(result, string.Empty);

        // Encode remaining HTML entities
        result = result
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;");

        return result.Trim();
    }

    /// <summary>
    /// Sanitizes plain text input — trims and limits length.
    /// </summary>
    public static string SanitizeText(string? input, int maxLength = 2000)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var trimmed = input.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    [GeneratedRegex(@"<script[^>]*>[\s\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"<style[^>]*>[\s\S]*?</style>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleTagRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"on\w+\s*=\s*""[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlerRegex();

    [GeneratedRegex(@"(javascript|data)\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex ProtocolRegex();
}
