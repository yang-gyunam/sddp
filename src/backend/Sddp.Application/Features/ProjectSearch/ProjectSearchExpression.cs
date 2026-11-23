using System.Text.RegularExpressions;
using Dapper;
using Sddp.Abstractions.Exceptions;

namespace Sddp.Application.Features.ProjectSearch;

/// <summary>Parse once on the server. Only validated operators and parameter names enter SQL.</summary>
public sealed class ProjectSearchExpression
{
    private sealed record Token(string Text, bool Literal, bool Quoted = false);
    private readonly Token[] _tokens;
    private readonly string _field;
    private int _position;
    private readonly List<string> _terms = [];
    public string[] Terms => _terms.ToArray();
    public string Predicate { get; }
    public DynamicParameters Parameters { get; } = new();

    // Reuse the lexer across requests and keep the assembly identical across staging paths.
    private static readonly Regex Lexer = new(@"""[^""]*""|[()]|[^\s()""]+|""", RegexOptions.CultureInvariant);

    public ProjectSearchExpression(string text, string mode, string field)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 240 || mode is not ("AND" or "OR") ||
            field is not ("all" or "title" or "body" or "path"))
            throw Invalid("Invalid search text, condition or field.");
        _field = field;
        var tokens = new List<Token>();
        foreach (Match match in Lexer.Matches(text))
        {
            var word = match.Value;
            if (string.IsNullOrWhiteSpace(word)) continue;
            if (word.StartsWith('"'))
            {
                if (word.Length < 3 || string.IsNullOrWhiteSpace(word[1..^1])) throw Invalid("따옴표 안에 검색할 문구를 입력하세요.");
                tokens.Add(new(word[1..^1], true, true));
            }
            else
            {
                var op = word.ToUpperInvariant();
                tokens.Add(op is "AND" or "OR" or "(" or ")" ? new(op, false) : new(word, true));
            }
        }
        var implicitOperator = tokens.Any(t => !t.Literal && t.Text is "AND" or "OR") ? "AND" : mode;
        var expanded = new List<Token>();
        foreach (var token in tokens)
        {
            var previous = expanded.LastOrDefault();
            if ((previous?.Literal == true || previous?.Text == ")") && (token.Literal || token.Text == "("))
                expanded.Add(new(implicitOperator, false));
            expanded.Add(token);
        }
        _tokens = expanded.ToArray();
        Predicate = Disjunction();
        if (_position != _tokens.Length) throw Invalid("검색식의 괄호를 확인하세요.");
    }

    private string Disjunction()
    {
        var sql = Conjunction();
        while (Take("OR")) sql = $"({sql} OR {Conjunction()})";
        return sql;
    }

    private string Conjunction()
    {
        var sql = Atom();
        while (Take("AND")) sql = $"({sql} AND {Atom()})";
        return sql;
    }

    private string Atom()
    {
        if (Take("("))
        {
            var inner = Disjunction();
            if (!Take(")")) throw Invalid("닫는 괄호를 입력하세요.");
            return inner;
        }
        if (_position == _tokens.Length || !_tokens[_position].Literal) throw Invalid("AND 또는 OR 양쪽에 검색어를 입력하세요.");
        var term = _tokens[_position++];
        var value = term.Text;
        var n = _terms.Count;
        _terms.Add(value);
        Parameters.Add($"t{n}", value);
        // Escaped LIKE supplies literal substring matches for Korean inflections, paths and identifiers.
        var escaped = value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        Parameters.Add($"p{n}", $"%{escaped}%");
        var title = $"(title ILIKE @p{n} OR code ILIKE @p{n})";
        var body = term.Quoted ? $"body ILIKE @p{n}" : $"(body ILIKE @p{n} OR to_tsvector('simple',left(body,60000)) @@ phraseto_tsquery('simple',@t{n}))";
        var path = $"(location ILIKE @p{n} OR status ILIKE @p{n} OR version ILIKE @p{n} OR menu ILIKE @p{n} OR kind ILIKE @p{n} OR to_char(updated_at AT TIME ZONE 'UTC','YYYY-MM-DD') LIKE @p{n})";
        return _field switch { "title" => title, "body" => body, "path" => path, _ => $"({title} OR {body} OR {path})" };
    }

    private bool Take(string op)
    {
        if (_position >= _tokens.Length || _tokens[_position].Literal || _tokens[_position].Text != op) return false;
        _position++;
        return true;
    }
    private static ValidationException Invalid(string message) => new("q", message);
}
