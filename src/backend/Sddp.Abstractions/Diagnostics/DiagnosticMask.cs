using System.Text;
using System.Text.RegularExpressions;

namespace Sddp.Diagnostics;

/// <summary>Shared masking at collection and persistence. The result remains restricted diagnostic data.</summary>
public static class DiagnosticMask
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(100);
    public const string Hidden = "[redacted]";

    private static readonly Regex Secrets = new("""(?ix)(?<prefix>["']?(?:password|passwd|pwd|authorization|proxy-authorization|cookie|set-cookie|api[-_]?key|access[-_]?token|refresh[-_]?token|client[-_]?secret|secret|token)["']?\s*[:=]\s*)(?:"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|[^\r\n;,}&]+)""", RegexOptions.CultureInvariant, Timeout);

    private static readonly Regex Auth = new("""(?i)\b(Bearer|Basic)\s+[A-Za-z0-9._~+/=-]+""", RegexOptions.CultureInvariant, Timeout);

    private static readonly Regex Email = new("""(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b""", RegexOptions.CultureInvariant, Timeout);

    private static readonly Regex Home = new("""(?i)(/Users/|/home/|[A-Z]:\\Users\\)[^/\\\s]+""", RegexOptions.CultureInvariant, Timeout);

    private static readonly Regex UserInfo = new("""(?i)(https?://)[^/\s@]+@""", RegexOptions.CultureInvariant, Timeout);

    private static readonly Regex QueryValues = new("""([?&][\w.%~-]+=)[^&#\s)]+""", RegexOptions.CultureInvariant, Timeout);

    private static readonly Regex SecretSegment = new("""(?i)(/(?:reset-password|password-reset|invite|token|activate)/)[^/?#\s]+""", RegexOptions.CultureInvariant, Timeout);

    public static string? Text(string? value, int limit = 4000)
    {
        if (value is null) return null;
        var text = value.Length <= limit ? value : value[..limit];
        try
        {
            text = UserInfo.Replace(text, "$1" + Hidden + "@");
            text = Auth.Replace(text, "$1 " + Hidden);
            text = Secrets.Replace(text, "${prefix}" + Hidden);
            text = Email.Replace(text, Hidden);
            text = Home.Replace(text, "$1" + Hidden);
            text = SecretSegment.Replace(text, "$1" + Hidden);
            text = QueryValues.Replace(text, "$1" + Hidden);
            return text.Length <= limit ? text : text[..limit];
        }
        catch (RegexMatchTimeoutException) { return Hidden; }
    }

    public static string? Url(string? value, int limit = 2000)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var end = value.IndexOfAny(['?', '#']);
        var path = end < 0 ? value : value[..end];
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            path = uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped);
        else path = Uri.UnescapeDataString(path);
        var masked = Text(path, limit);
        return masked;
    }

    public static bool IsSensitiveKey(string key) =>
        key.Contains("password", StringComparison.OrdinalIgnoreCase) || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
        || key.Contains("token", StringComparison.OrdinalIgnoreCase) || key.Contains("cookie", StringComparison.OrdinalIgnoreCase)
        || key.Contains("authorization", StringComparison.OrdinalIgnoreCase) || key.Contains("api-key", StringComparison.OrdinalIgnoreCase)
        || key.Contains("api_key", StringComparison.OrdinalIgnoreCase) || key.Contains("apikey", StringComparison.OrdinalIgnoreCase);

    // PostgreSQL values, parameters and comments are distinct from identifiers and operators.
    public static string? Sql(string? sql)
    {
        if (sql is null) return null;
        if (sql.Length > 65536) return "[large SQL statement]";
        var output = new StringBuilder(sql.Length);
        for (var i = 0; i < sql.Length;)
        {
            var c = sql[i++];
            if (c == '\'')
            {
                output.Append('?');
                var escapes = i >= 2 && sql[i - 2] is 'e' or 'E' && (i == 2 || !char.IsLetterOrDigit(sql[i - 3]));
                while (i < sql.Length)
                {
                    var next = sql[i++];
                    if (escapes && next == '\\' && i < sql.Length) { i++; continue; }
                    if (next != '\'') continue;
                    if (i >= sql.Length || sql[i] != '\'') break;
                    i++;
                }
            }
            else if (c == '"')
            {
                var begin = i - 1;
                while (i < sql.Length)
                {
                    if (sql[i++] != '"') continue;
                    if (i == sql.Length || sql[i] != '"') break;
                    i++;
                }
                output.Append(sql, begin, i - begin);
            }
            else if (c == '-' && i < sql.Length && sql[i] == '-')
            {
                while (i < sql.Length && sql[i] != '\n') i++;
                output.Append(' ');
            }
            else if (c == '/' && i < sql.Length && sql[i] == '*')
            {
                i++;
                int depth = 1;
                while (i < sql.Length && depth > 0)
                {
                    if (sql.AsSpan(i).StartsWith("/*")) { depth++; i += 2; }
                    else if (sql.AsSpan(i).StartsWith("*/")) { depth--; i += 2; }
                    else i++;
                }
                output.Append(' ');
            }
            else if (c == '$' && i < sql.Length && (sql[i] == '$' || sql[i] == '_' || char.IsAsciiLetter(sql[i])))
            {
                var start = i - 1;
                var j = i;
                while (j < sql.Length && (sql[j] == '_' || char.IsAsciiLetterOrDigit(sql[j]))) j++;
                if (j == sql.Length || sql[j] != '$') { output.Append(c); continue; }
                var tag = sql[start..(j + 1)];
                var close = sql.IndexOf(tag, j + 1, StringComparison.Ordinal);
                i = close < 0 ? sql.Length : close + tag.Length;
                output.Append('?');
            }
            else if (char.IsAsciiDigit(c) && (i == 1 || !(char.IsLetterOrDigit(sql[i - 2]) || sql[i - 2] is '_' or '$')))
            {
                while (i < sql.Length && (char.IsAsciiLetterOrDigit(sql[i]) || sql[i] is '.' or '_')) i++;
                output.Append('?');
            }
            else output.Append(c);
        }
        return output.ToString();
    }
}
