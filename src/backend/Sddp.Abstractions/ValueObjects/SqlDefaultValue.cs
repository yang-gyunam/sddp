using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Sddp.Abstractions.ValueObjects;

public static class SqlDefaultValue
{
    /// <summary>
    /// The single expression an author may ask for, because "now" is the one default that cannot
    /// be written as a literal and is worth having. Matched exactly, not by prefix.
    /// </summary>
    private const string NowExpression = "NOW()";

    /// <summary>
    /// Renders <paramref name="value"/> as a literal of <paramref name="fieldType"/>, or fails.
    /// </summary>
    public static bool TryRender(
        string? value,
        string fieldType,
        [NotNullWhen(true)] out string? sql)
    {
        sql = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        switch (fieldType)
        {
            case "String":
            case "Json":
                // Doubling the quote is what closes the hole; PostgreSQL reads '' inside a literal
                // as one apostrophe, so "O'Brien" survives intact instead of ending the string.
                sql = $"'{trimmed.Replace("'", "''", StringComparison.Ordinal)}'";
                return true;

            case "Boolean":
                if (!bool.TryParse(trimmed, out var flag))
                {
                    return false;
                }
                sql = flag ? "true" : "false";
                return true;

            case "Int":
                if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                {
                    return false;
                }
                sql = i.ToString(CultureInfo.InvariantCulture);
                return true;

            case "Long":
                if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                {
                    return false;
                }
                sql = l.ToString(CultureInfo.InvariantCulture);
                return true;

            case "Decimal":
                if (!decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                {
                    return false;
                }
                sql = d.ToString(CultureInfo.InvariantCulture);
                return true;

            case "DateTime":
                if (string.Equals(trimmed, NowExpression, StringComparison.OrdinalIgnoreCase))
                {
                    sql = NowExpression;
                    return true;
                }
                if (!DateTimeOffset.TryParse(
                        trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var at))
                {
                    return false;
                }
                sql = $"'{at.ToString("o", CultureInfo.InvariantCulture)}'";
                return true;

            case "Guid":
                if (!Guid.TryParse(trimmed, out var guid))
                {
                    return false;
                }
                sql = $"'{guid.ToString("D", CultureInfo.InvariantCulture)}'";
                return true;

            default:
                // An unknown type has no rendering rule, and guessing one is how the raw fall-
                // through got there in the first place.
                return false;
        }
    }

    public static string Render(string? value, string fieldType) =>
        TryRender(value, fieldType, out var sql)
            ? sql
            : throw new FormatException(
                $"Default '{value}' is not a valid {fieldType} value. Defaults are written as "
                + "literals of their column's type; SQL expressions are not accepted (except NOW() "
                + "for DateTime).");
}
