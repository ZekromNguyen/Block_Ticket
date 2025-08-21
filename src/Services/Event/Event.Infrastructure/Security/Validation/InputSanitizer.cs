using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Web;

namespace Event.Infrastructure.Security.Validation;

/// <summary>
/// Provides input sanitization to prevent XSS and other injection attacks
/// </summary>
public static class InputSanitizer
{
    private static readonly HtmlEncoder _htmlEncoder = HtmlEncoder.Default;
    private static readonly JavaScriptEncoder _jsEncoder = JavaScriptEncoder.Default;
    private static readonly UrlEncoder _urlEncoder = UrlEncoder.Default;

    // Regex patterns for detecting potentially malicious content
    private static readonly Regex ScriptTagRegex = new(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex OnEventRegex = new(@"on\w+\s*=", RegexOptions.IgnoreCase);
    private static readonly Regex JavaScriptUrlRegex = new(@"javascript\s*:", RegexOptions.IgnoreCase);
    private static readonly Regex SqlInjectionRegex = new(@"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|UNION|SCRIPT)\b)", RegexOptions.IgnoreCase);
    private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.IgnoreCase);

    /// <summary>
    /// Sanitizes HTML content by encoding dangerous characters
    /// </summary>
    public static string SanitizeHtml(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // First pass: remove script tags entirely
        var sanitized = ScriptTagRegex.Replace(input, string.Empty);

        // Remove on* event handlers
        sanitized = OnEventRegex.Replace(sanitized, string.Empty);

        // Remove javascript: URLs
        sanitized = JavaScriptUrlRegex.Replace(sanitized, string.Empty);

        // HTML encode the result
        return _htmlEncoder.Encode(sanitized);
    }

    /// <summary>
    /// Sanitizes input for use in JavaScript contexts
    /// </summary>
    public static string SanitizeJavaScript(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return _jsEncoder.Encode(input);
    }

    /// <summary>
    /// Sanitizes input for use in URLs
    /// </summary>
    public static string SanitizeUrl(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Check for dangerous URL schemes
        if (JavaScriptUrlRegex.IsMatch(input) || input.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return _urlEncoder.Encode(input);
    }

    /// <summary>
    /// Removes all HTML tags from input
    /// </summary>
    public static string StripHtml(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return HtmlTagRegex.Replace(input, string.Empty);
    }

    /// <summary>
    /// Sanitizes input to prevent SQL injection
    /// </summary>
    public static string SanitizeSql(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // This is a basic check - proper parameterized queries are the real protection
        if (SqlInjectionRegex.IsMatch(input))
        {
            throw new ArgumentException("Input contains potentially dangerous SQL keywords");
        }

        return input.Replace("'", "''"); // Basic SQL escaping
    }

    /// <summary>
    /// Sanitizes general text input
    /// </summary>
    public static string SanitizeText(string? input, bool allowHtml = false)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        if (allowHtml)
        {
            return SanitizeHtml(input);
        }
        else
        {
            return StripHtml(input);
        }
    }

    /// <summary>
    /// Validates and sanitizes email addresses
    /// </summary>
    public static string SanitizeEmail(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Basic email validation regex
        var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        
        var sanitized = input.Trim().ToLowerInvariant();
        
        if (!emailRegex.IsMatch(sanitized))
        {
            throw new ArgumentException("Invalid email format");
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes phone numbers
    /// </summary>
    public static string SanitizePhoneNumber(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove all non-digit characters except + at the beginning
        var sanitized = Regex.Replace(input, @"[^\d+]", string.Empty);
        
        // Ensure + is only at the beginning
        if (sanitized.Contains('+'))
        {
            var parts = sanitized.Split('+');
            sanitized = "+" + string.Join(string.Empty, parts.Skip(1));
        }

        return sanitized;
    }

    /// <summary>
    /// Validates and sanitizes timezone identifiers
    /// </summary>
    public static string SanitizeTimeZone(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return "UTC";

        try
        {
            // Validate that it's a valid timezone
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(input);
            return timeZone.Id;
        }
        catch
        {
            // Fall back to UTC if invalid
            return "UTC";
        }
    }

    /// <summary>
    /// Sanitizes file names to prevent path traversal attacks
    /// </summary>
    public static string SanitizeFileName(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove path separators and other dangerous characters
        var invalidChars = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' });
        var sanitized = string.Join("_", input.Split(invalidChars.ToArray(), StringSplitOptions.RemoveEmptyEntries));

        // Prevent directory traversal
        sanitized = sanitized.Replace("..", "_");

        // Limit length
        if (sanitized.Length > 255)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExtension.Substring(0, 255 - extension.Length) + extension;
        }

        return sanitized;
    }

    /// <summary>
    /// Checks if input contains potentially malicious content
    /// </summary>
    public static bool IsPotentiallyMalicious(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return ScriptTagRegex.IsMatch(input) ||
               OnEventRegex.IsMatch(input) ||
               JavaScriptUrlRegex.IsMatch(input) ||
               SqlInjectionRegex.IsMatch(input);
    }

    /// <summary>
    /// Truncates text to a maximum length while preserving word boundaries
    /// </summary>
    public static string TruncateText(string? input, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input ?? string.Empty;

        var truncated = input.Substring(0, maxLength - suffix.Length);
        var lastSpace = truncated.LastIndexOf(' ');
        
        if (lastSpace > 0)
        {
            truncated = truncated.Substring(0, lastSpace);
        }

        return truncated + suffix;
    }

    /// <summary>
    /// Normalizes whitespace in text
    /// </summary>
    public static string NormalizeWhitespace(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Replace multiple whitespace characters with single spaces
        return Regex.Replace(input.Trim(), @"\s+", " ");
    }
}
