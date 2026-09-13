using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Sddp.Abstractions.ValueObjects;

public readonly record struct SqlIdentifier
{
    /// <summary>
    /// PostgreSQL truncates identifiers at 63 bytes rather than refusing them, which turns two long
    /// distinct names into one silently.
    /// </summary>
    public const int MaxLength = 63;

    private static readonly Regex Canonical = new(
        @"^[a-z_][a-z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex Acceptable = new(
        @"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private SqlIdentifier(string value) => Value = value;

    /// <summary>The canonical (lower-case) form. Always safe to quote and emit.</summary>
    public string Value { get; }

    public override string ToString() => Value;

    /// <summary>
    /// The form to write into SQL. Quoted, so a name that collides with a reserved word
    /// (<c>order</c>, <c>user</c>, <c>table</c>) is still usable.
    /// </summary>
    public string Quoted => $"\"{Value}\"";

    /// <summary>
    /// Parses <paramref name="value"/>, folding case the way PostgreSQL folds an unquoted
    /// identifier. Mixed case is accepted rather than refused because that is what an unquoted
    /// name already did — <c>MyTable</c> has always become <c>mytable</c> — and refusing it now
    /// would reject metadata that exists.
    /// </summary>
    public static bool TryParse(string? value, [NotNullWhen(true)] out SqlIdentifier? identifier)
    {
        identifier = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        // Length is measured in bytes, which is what PostgreSQL truncates on. A name of 63
        // characters can still be 126 bytes.
        if (Encoding.UTF8.GetByteCount(trimmed) > MaxLength || !Acceptable.IsMatch(trimmed))
        {
            return false;
        }

        var canonical = trimmed.ToLowerInvariant();
        if (!Canonical.IsMatch(canonical))
        {
            return false;
        }

        identifier = new SqlIdentifier(canonical);
        return true;
    }

    public static SqlIdentifier Parse(string? value, string what = "identifier") =>
        TryParse(value, out var identifier)
            ? identifier.Value
            : throw new FormatException(
                $"{what} '{value}' is not a usable SQL name. Use letters, digits and underscores, "
                + $"start with a letter or underscore, and stay within {MaxLength} bytes.");

    /// <summary>
    /// Builds a derived name (an index, say) that fits.
    ///
    /// Plain truncation would map two long column names onto one index name, and
    /// <c>CREATE INDEX IF NOT EXISTS</c> would then skip the second one without complaint — the
    /// unique constraint an author asked for would simply not exist. Truncating with a digest of
    /// the full name keeps distinct inputs distinct.
    /// </summary>
    public static SqlIdentifier Derive(string prefix, params SqlIdentifier[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var joined = string.Join('_', new[] { prefix }.Concat(parts.Select(part => part.Value)));
        var canonical = joined.ToLowerInvariant();

        if (Encoding.UTF8.GetByteCount(canonical) <= MaxLength && Canonical.IsMatch(canonical))
        {
            return new SqlIdentifier(canonical);
        }

        var digest = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant()[..8];

        var room = MaxLength - digest.Length - 1;
        var head = canonical.Length <= room ? canonical : canonical[..room];

        return new SqlIdentifier(
            string.Create(CultureInfo.InvariantCulture, $"{head.TrimEnd('_')}_{digest}"));
    }
}
