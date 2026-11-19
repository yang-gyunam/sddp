using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Sddp.Abstractions.Models.Diagrams;
using Sddp.Abstractions.Models.Diagrams.V2;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Infrastructure.Services.Diagrams;

/// <summary>SDDP profile1 grammar, not an adapter to Mermaid private APIs or a full Mermaid parser.</summary>
public sealed class MermaidSubsetParser2 : IDiagramParser2
{
    private const string Id = @"[A-Za-z][A-Za-z0-9_]*";
    public DiagramParserCapability2 Capability { get; } = new("sddp-mermaid-subset", "1.0.0",
        new[] { "flowchart", "sequence", "class", "state", "er", "c4.context", "c4.container" }
            .Select(kind => new DiagramProfile2 { Id = "sddp.mermaid." + kind, Version = 1 }).ToArray(), false);
    private static Match Match(string text, string pattern) => Regex.Match(text, "^(?:" + pattern + ")$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static DiagramPresentation2 Presentation() => DiagramBridge2.Presentation(new());

    public DiagramParseResult2 Parse(string text, DiagramProfile2? profile, string diagramId, int revision)
    {
        var context = new Context(text, diagramId, revision);
        var validating = false;
        try
        {
            if (Encoding.UTF8.GetByteCount(text) > 2 * 1024 * 1024) throw new InvalidDataException("Maximum source size is 2 MiB.");
            var lines = Regex.Matches(text, @"[^\r\n]+|\r\n|\r|\n").Where(m => m.Value is not ("\r\n" or "\r" or "\n"))
                .Select(m => new Line(m.Value.Trim(), m.Index + m.Value.Length - m.Value.TrimStart().Length, m.Index + m.Value.TrimEnd().Length)).Where(l => l.Text.Length > 0).ToList();
            var body = new List<Line>();
            foreach (var line in lines)
            {
                context.Line = line;
                if (line.Text.StartsWith("%%{", StringComparison.Ordinal)) throw new InvalidDataException("Unsupported configuration directive.");
                if (line.Text.StartsWith("%%", StringComparison.Ordinal)) continue;
                body.Add(line);
            }
            if (body.Count == 0) throw new InvalidDataException("Expected diagram header.");
            context.Line = body[0]; var header = body[0].Text;
            var kind = header switch
            {
                var flow when Match(flow, @"flowchart\s+(?:LR|TD)").Success => "flowchart", "sequenceDiagram" => "sequence",
                "classDiagram" => "class", "stateDiagram-v2" => "state", "erDiagram" => "er",
                "C4Context" => "c4.context", "C4Container" => "c4.container",
                _ => throw new InvalidDataException("Unsupported diagram header.")
            };
            var expected = "sddp.mermaid." + kind;
            if (profile is not null && (profile.Id != expected || profile.Version != 1))
                return new(null, [new("UNSUPPORTED_PROFILE", "error", "Header/profile mismatch or unsupported profile version.", context.Range())]);
            context.Profile = new() { Id = expected, Version = 1 };
            var rest = body.Skip(1).ToList();
            DiagramContent2 content = kind switch
            {
                "flowchart" => Flowchart(context, rest), "sequence" => Sequence(context, rest),
                "class" => Classes(context, rest), "state" => States(context, rest),
                "er" => Er(context, rest), _ => C4(context, rest, kind == "c4.container")
            };
            var model = context.Model(kind.StartsWith("c4", StringComparison.Ordinal) ? "c4" : kind, content);
            if (kind == "flowchart") model.Presentation.Properties["direction"] = header.EndsWith("LR", StringComparison.Ordinal) ? "LR" : "TD";
            validating = true;
            DiagramValidation2.Validate(model);
            return new(model, []);
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or RegexMatchTimeoutException)
        {
            return new(null, [new((ex.Message.StartsWith("INVALID_", StringComparison.Ordinal) || ex.Message.StartsWith("IDENTITY_", StringComparison.Ordinal)) ? ex.Message.Split(':')[0] : "UNSUPPORTED_SYNTAX", "error", ex.Message, validating ? null : context.Range())]);
        }
    }

    private sealed record Line(string Text, int Start, int End);
    private sealed class Context(string text, string diagramId, int revision)
    {
        public Line Line { get; set; } = new("", 0, 0);
        public DiagramProfile2 Profile { get; set; } = new() { Id = "", Version = 1 };
        public List<DiagramBinding2> Bindings { get; } = [];
        public Dictionary<(string Collection, string Source), string> Names { get; } = new();
        public DiagramSourceRange2 Range() => new() { Start = Line.Start, End = Line.End };
        public string Add(string collection, string? source = null, string[]? key = null)
        {
            var id = GlobalUniqueId.NewId().ToString();
            if (source is not null)
            {
                if (!Match(source, Id).Success || IsKeyword(source)) throw new InvalidDataException("Unsupported or reserved source identifier.");
                if (Profile.Id.StartsWith("sddp.mermaid.c4.", StringComparison.Ordinal) && Names.Keys.Any(k => k.Source == source))
                    throw new InvalidDataException("IDENTITY_CONFLICT: duplicate C4 source identifier.");
                if (!Names.TryAdd((collection, source), id)) throw new InvalidDataException("IDENTITY_CONFLICT: duplicate source identifier.");
                key ??= [collection, source];
            }
            Bindings.Add(new() { Element = new() { Collection = collection, Id = id }, Key = key?.ToList(), Range = Range() });
            return id;
        }
        private bool IsKeyword(string value) => Profile.Id switch
        {
            "sddp.mermaid.flowchart" => new[] { "flowchart", "graph", "subgraph", "end", "style", "click", "classDef" }.Contains(value),
            "sddp.mermaid.sequence" => new[] { "participant", "actor", "activate", "deactivate", "alt", "opt", "loop", "else", "end", "par", "critical", "note" }.Contains(value),
            "sddp.mermaid.class" => new[] { "class", "classDiagram", "namespace", "note" }.Contains(value),
            "sddp.mermaid.state" => new[] { "state", "note", "direction" }.Contains(value),
            _ => false
        };
        public string Resolve(string collection, string source) => Names.GetValueOrDefault((collection, source))
            ?? throw new InvalidDataException($"INVALID_REFERENCE: undeclared {source}.");
        public DiagramRevision2 Model(string kind, DiagramContent2 content) => new()
        {
            SchemaVersion = 2, DiagramId = diagramId, Revision = revision, DiagramType = kind,
            Source = new() { Format = "mermaid", Provider = "sddp-mermaid-subset", ProviderVersion = "1.0.0", Text = text },
            SourceState = "aligned", SourceMap = new()
            {
                SourceHash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))),
                Profile = Profile, Adapter = new() { Id = "sddp-mermaid-subset", Version = "1.0.0" }, Bindings = Bindings
            },
            Presentation = Presentation(), Content = content
        };
    }

    private static string Text(string value, bool empty = false)
    {
        if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2) value = value[1..^1];
        if ((!empty && value.Length == 0) || value != value.Trim() || value.Any(char.IsControl) || value.IndexOfAny(['[', ']', '{', '}', '|', '`', '"', '\\']) >= 0
            || Regex.IsMatch(value, @"<[/!A-Za-z][^>]*>|&(?:#\w+|\w+);|\*\*|__", RegexOptions.CultureInvariant))
            throw new InvalidDataException("Unsupported text/escape/markup in profile1.");
        return value;
    }

    private static FlowchartContent2 Flowchart(Context c, List<Line> lines)
    {
        var nodes = new Dictionary<string, FlowNode2>(); var edges = new Dictionary<string, FlowEdge2>();
        var connections = new List<Line>();
        foreach (var line in lines)
        {
            c.Line = line;
            if (Regex.IsMatch(line.Text, @"^(?:[A-Za-z][A-Za-z0-9_]*|\[\*\])\s+-->", RegexOptions.CultureInvariant)) { connections.Add(line); continue; }
            var m = Match(line.Text, $"(?<id>{Id})(?<shape>\\(\\[.*\\]\\)|\\[.*\\]|\\{{.*\\}})?");
            if (!m.Success) throw new InvalidDataException("Unsupported flowchart statement.");
            var source = m.Groups["id"].Value; var shape = m.Groups["shape"].Value;
            var kind = shape.StartsWith("([", StringComparison.Ordinal) ? "terminator" : shape.StartsWith('{') ? "decision" : "process";
            var label = shape.Length == 0 ? source : kind == "terminator" ? shape[2..^2] : shape[1..^1];
            nodes.Add(c.Add("nodes", source), new() { Name = Text(label), Kind = kind, Presentation = Presentation() });
        }
        var pairs = new HashSet<(string, string)>();
        foreach (var line in connections)
        {
            c.Line = line; var m = Match(line.Text, $"(?<from>{Id})\\s+-->\\s*(?:\\|(?<label>[^|]+)\\|\\s*)?(?<to>{Id})");
            if (!m.Success) throw new InvalidDataException("Unsupported flowchart connection.");
            var from = c.Resolve("nodes", m.Groups["from"].Value); var to = c.Resolve("nodes", m.Groups["to"].Value);
            if (!pairs.Add((from, to))) throw new InvalidDataException("IDENTITY_AMBIGUOUS: parallel implicit flowchart edges.");
            edges.Add(c.Add("edges"), new() { From = from, To = to, Label = m.Groups["label"].Success ? Text(m.Groups["label"].Value) : "", Presentation = Presentation() });
        }
        return new() { Nodes = nodes, Edges = edges };
    }

    private static StateContent2 States(Context c, List<Line> lines)
    {
        var states = new Dictionary<string, StateNode2>(); var transitions = new Dictionary<string, StateTransition2>(); var connections = new List<Line>();
        foreach (var line in lines)
        {
            c.Line = line;
            if (Regex.IsMatch(line.Text, @"^(?:[A-Za-z][A-Za-z0-9_]*|\[\*\])\s+-->", RegexOptions.CultureInvariant)) { connections.Add(line); continue; }
            var m = Match(line.Text, $"state\\s+\\\"(?<name>[^\\\"]+)\\\"\\s+as\\s+(?<id>{Id})|state\\s+(?<id>{Id})|(?<id>{Id}):\\s*(?<name>.+)");
            if (!m.Success) throw new InvalidDataException("Unsupported state declaration.");
            var source = m.Groups["id"].Value;
            states.Add(c.Add("states", source), new() { Name = Text(m.Groups["name"].Success ? m.Groups["name"].Value : source), Kind = "state", Presentation = Presentation() });
        }
        string Endpoint(string source, bool initial)
        {
            if (source != "[*]") return c.Resolve("states", source);
            var key = initial ? "initial" : "final";
            var existing = states.FirstOrDefault(s => s.Value.Kind == key);
            if (existing.Key is not null) return existing.Key;
            var id = c.Add("states", key: ["root", key]); states.Add(id, new() { Name = "", Kind = key, Presentation = Presentation() }); return id;
        }
        foreach (var line in connections)
        {
            c.Line = line; var m = Match(line.Text, $"(?<from>{Id}|\\[\\*\\])\\s+-->\\s+(?<to>{Id}|\\[\\*\\])(?:\\s*:\\s*(?<event>.+))?");
            if (!m.Success) throw new InvalidDataException("Unsupported state transition.");
            var label = m.Groups["event"].Success ? Text(m.Groups["event"].Value) : "";
            if (label.Contains('/') || label.StartsWith("do ", StringComparison.Ordinal) ||
                Regex.IsMatch(label, @"^(?:entry|exit)(?:\s|$)", RegexOptions.CultureInvariant))
                throw new InvalidDataException("Unsupported state guard/action.");
            transitions.Add(c.Add("transitions"), new() { From = Endpoint(m.Groups["from"].Value, true), To = Endpoint(m.Groups["to"].Value, false), Event = label, Presentation = Presentation() });
        }
        return new() { States = states, Transitions = transitions };
    }

    private static SequenceContent2 Sequence(Context c, List<Line> lines)
    {
        var participants = new Dictionary<string, SequenceParticipant2>(); var events = new Dictionary<string, SequenceEvent2>();
        var branches = new Dictionary<string, SequenceBranch2>(); var timeline = new List<string>(); var current = timeline;
        var frames = new Stack<(string Fragment, List<string> Parent)>(); var active = new Dictionary<(List<string>, string), Stack<string>>();
        var body = false;
        foreach (var line in lines)
        {
            c.Line = line;
            var m = Match(line.Text, $"(?<kind>actor|participant)\\s+(?<id>{Id})(?:\\s+as\\s+(?<name>.+))?");
            if (m.Success)
            {
                if (body) throw new InvalidDataException("Participant declarations must precede the timeline.");
                var source = m.Groups["id"].Value;
                participants.Add(c.Add("participants", source), new() { Name = Text(m.Groups["name"].Success ? m.Groups["name"].Value : source), Kind = m.Groups["kind"].Value, Presentation = Presentation() }); continue;
            }
            body = true;
            m = Match(line.Text, $"(?<from>{Id})(?<arrow>-->>|->>)(?<to>{Id}):\\s*(?<text>.+)");
            if (m.Success)
            {
                var id = c.Add("events"); events.Add(id, new SequenceMessage2
                {
                    From = c.Resolve("participants", m.Groups["from"].Value), To = c.Resolve("participants", m.Groups["to"].Value),
                    Kind = m.Groups["arrow"].Value == "->>" ? "solidArrow" : "dashedArrow", Text = Text(m.Groups["text"].Value), Presentation = Presentation()
                }); current.Add(id); continue;
            }
            m = Match(line.Text, $"(?<verb>activate|deactivate)\\s+(?<id>{Id})");
            if (m.Success)
            {
                var participant = c.Resolve("participants", m.Groups["id"].Value); var id = c.Add("events");
                if (!active.TryGetValue((current, participant), out var stack)) active[(current, participant)] = stack = new();
                if (m.Groups["verb"].Value == "activate") { events.Add(id, new SequenceActivationStart2 { Participant = participant, Presentation = Presentation() }); stack.Push(id); }
                else
                {
                    if (stack.Count == 0) throw new InvalidDataException("INVALID_ORDER: activation underflow/scope.");
                    events.Add(id, new SequenceActivationEnd2 { Start = stack.Pop(), Presentation = Presentation() });
                }
                current.Add(id); continue;
            }
            m = Match(line.Text, @"(?<kind>alt|opt|loop)\s+(?<label>.+)");
            if (m.Success)
            {
                var kind = m.Groups["kind"].Value; var branch = c.Add("branches"); var fragment = c.Add("events");
                branches.Add(branch, new() { Kind = kind == "alt" ? "condition" : "body", Label = Text(m.Groups["label"].Value), Events = [], Presentation = Presentation() });
                events.Add(fragment, new SequenceFragment2 { Kind = kind, BranchOrder = [branch], Presentation = Presentation() });
                current.Add(fragment); frames.Push((fragment, current)); current = branches[branch].Events;
                if (frames.Count > 64) throw new InvalidDataException("INVALID_ORDER: fragment nesting limit."); continue;
            }
            m = Match(line.Text, @"else\s+(?<label>.+)");
            if (m.Success)
            {
                if (frames.Count == 0 || events[frames.Peek().Fragment] is not SequenceFragment2 { Kind: "alt", BranchOrder.Count: 1 } fragment)
                    throw new InvalidDataException("INVALID_ORDER: unexpected else.");
                var id = c.Add("branches"); branches.Add(id, new() { Kind = "else", Label = Text(m.Groups["label"].Value), Events = [], Presentation = Presentation() });
                fragment.BranchOrder.Add(id); current = branches[id].Events; continue;
            }
            if (line.Text == "end")
            {
                if (!frames.TryPop(out var frame)) throw new InvalidDataException("INVALID_ORDER: unexpected end.");
                var binding = c.Bindings.Single(b => b.Element.Collection == "events" && b.Element.Id == frame.Fragment);
                var index = c.Bindings.IndexOf(binding); c.Bindings[index] = binding with { Range = new() { Start = binding.Range!.Start, End = line.End } };
                current = frame.Parent; continue;
            }
            throw new InvalidDataException("Unsupported sequence statement.");
        }
        if (frames.Count > 0 || active.Values.Any(stack => stack.Count > 0)) throw new InvalidDataException("INVALID_ORDER: unclosed fragment/activation.");
        return new() { Participants = participants, Events = events, Branches = branches, Timeline = timeline };
    }

    private static string Visibility(string text) => text switch { "+" => "public", "-" => "private", "#" => "protected", "~" => "internal", _ => "unspecified" };

    private static ClassContent2 Classes(Context c, List<Line> lines)
    {
        var types = new Dictionary<string, ClassType2>(); var attributes = new Dictionary<string, ClassAttribute2>();
        var operations = new Dictionary<string, ClassOperation2>(); var parameters = new Dictionary<string, ClassParameter2>();
        var relations = new Dictionary<string, ClassRelation2>(); var links = new List<Line>();
        var display = new HashSet<string>(); var blocks = new HashSet<string>(); var bare = new HashSet<string>();
        string? owner = null; string? ownerSource = null;
        foreach (var line in lines)
        {
            c.Line = line;
            if (line.Text == "}") { if (owner is null) throw new InvalidDataException("Unexpected class block end."); owner = ownerSource = null; continue; }
            if (owner is null)
            {
                var m = Match(line.Text, $"class\\s+(?<id>{Id})(?:\\[\\\"(?<name>[^\\\"]+)\\\"\\])?(?<block>\\s+\\{{)?");
                if (!m.Success) { links.Add(line); continue; }
                var source = m.Groups["id"].Value;
                var id = c.Names.GetValueOrDefault(("types", source));
                if (id is null) { id = c.Add("types", source); types.Add(id, new() { Name = source, Presentation = Presentation() }); }
                if (m.Groups["name"].Success)
                {
                    if (!display.Add(source)) throw new InvalidDataException("IDENTITY_CONFLICT: duplicate class display declaration.");
                    types[id] = types[id] with { Name = Text(m.Groups["name"].Value) };
                }
                if (m.Groups["block"].Success)
                {
                    if (!blocks.Add(source)) throw new InvalidDataException("IDENTITY_CONFLICT: duplicate class block.");
                    owner = id; ownerSource = source;
                }
                else if (!m.Groups["name"].Success && !bare.Add(source)) throw new InvalidDataException("Duplicate class declaration.");
                continue;
            }
            var method = Match(line.Text, $"(?<visibility>[+\\-#~]?)(?<name>{Id})\\((?<parameters>[^()]*)\\)(?:\\s+(?<return>{Id}))?");
            if (method.Success)
            {
                var args = method.Groups["parameters"].Value;
                var parts = args.Length == 0 ? [] : args.Split(',');
                var parsed = parts.Select(part => Match(part.Trim(), $"(?:(?<type>{Id})\\s+)?(?<name>{Id})")).ToArray();
                if (parsed.Any(part => !part.Success)) throw new InvalidDataException("Unsupported method parameter.");
                var known = parsed.All(part => part.Groups["type"].Success);
                string[]? key = known ? ["operations", ownerSource!, method.Groups["name"].Value, .. parsed.Select(part => part.Groups["type"].Value)] : null;
                var op = c.Add("operations", key: key); var order = new List<string>();
                var offset = line.Start + method.Groups["parameters"].Index;
                for (var i = 0; i < parsed.Length; i++)
                {
                    var part = parsed[i];
                    var name = part.Groups["name"].Value; var id = c.Add("parameters", key: key is null ? null : [.. key, "parameter", name]);
                    parameters.Add(id, new() { Name = name, TypeName = part.Groups["type"].Success ? part.Groups["type"].Value : null, Presentation = Presentation() }); order.Add(id);
                    // Parameter ranges distinguish anonymous parameters on the same line.
                    var raw = parts[i];
                    var start = offset + raw.Length - raw.TrimStart().Length;
                    var binding = c.Bindings[^1];
                    c.Bindings[^1] = binding with { Range = new() { Start = start, End = start + part.Value.Length } };
                    offset += raw.Length + 1;
                }
                operations.Add(op, new() { Owner = owner, Name = method.Groups["name"].Value, Visibility = Visibility(method.Groups["visibility"].Value), ReturnType = method.Groups["return"].Success ? method.Groups["return"].Value : null, ParameterOrder = order, Presentation = Presentation() }); continue;
            }
            var member = Match(line.Text, $"(?<visibility>[+\\-#~]?)(?<type>{Id})\\s+(?<name>{Id})");
            if (!member.Success) throw new InvalidDataException("Unsupported class member.");
            var aid = c.Add("attributes", key: ["attributes", ownerSource!, member.Groups["name"].Value]);
            attributes.Add(aid, new() { Owner = owner, Name = member.Groups["name"].Value, TypeName = member.Groups["type"].Value, Visibility = Visibility(member.Groups["visibility"].Value), Presentation = Presentation() });
        }
        if (owner is not null) throw new InvalidDataException("Unclosed class block.");
        foreach (var line in links)
        {
            c.Line = line; var m = Match(line.Text, $"(?<from>{Id})\\s+(?<arrow><\\|--|\\*--|o--|-->|\\.\\.>|\\.\\.\\|>)\\s+(?<to>{Id})(?:\\s*:\\s*(?<label>.+))?");
            if (!m.Success) throw new InvalidDataException("Unsupported class relationship.");
            var from = c.Resolve("types", m.Groups["from"].Value); var to = c.Resolve("types", m.Groups["to"].Value); var arrow = m.Groups["arrow"].Value;
            var kind = arrow switch { "<|--" => "inheritance", "*--" => "composition", "o--" => "aggregation", "-->" => "association", "..>" => "dependency", _ => "realization" };
            if (kind == "inheritance") (from, to) = (to, from);
            relations.Add(c.Add("relations"), new() { From = from, To = to, Kind = kind, Label = m.Groups["label"].Success ? Text(m.Groups["label"].Value) : "", Presentation = Presentation() });
        }
        return new() { Types = types, Attributes = attributes, Operations = operations, Parameters = parameters, Relations = relations };
    }

    private static ErContent2 Er(Context c, List<Line> lines)
    {
        var entities = new Dictionary<string, ErEntity2>(); var attributes = new Dictionary<string, ErAttribute2>();
        var relations = new Dictionary<string, ErRelation2>(); var links = new List<Line>(); string? owner = null; string? sourceOwner = null;
        foreach (var line in lines)
        {
            c.Line = line;
            if (line.Text == "}") { if (owner is null) throw new InvalidDataException("Unexpected entity block end."); owner = sourceOwner = null; continue; }
            if (owner is null)
            {
                var m = Match(line.Text, $"(?<id>{Id})(?:\\[\\\"(?<name>[^\\\"]+)\\\"\\])?(?<block>\\s+\\{{)?");
                if (!m.Success) { links.Add(line); continue; }
                var source = m.Groups["id"].Value; var id = c.Add("entities", source);
                entities.Add(id, new() { Name = Text(m.Groups["name"].Success ? m.Groups["name"].Value : source), Presentation = Presentation() });
                if (m.Groups["block"].Success) { owner = id; sourceOwner = source; } continue;
            }
            var member = Match(line.Text, $"(?<type>{Id})\\s+(?<name>{Id})(?:\\s+(?<keys>(?:PK|FK|UK)(?:\\s*,\\s*(?:PK|FK|UK))*))?");
            if (!member.Success) throw new InvalidDataException("Unsupported entity attribute.");
            var keys = member.Groups["keys"].Success ? member.Groups["keys"].Value.Split(',').Select(k => k.Trim() switch { "PK" => "primary", "FK" => "foreign", _ => "unique" }).ToList() : [];
            var aid = c.Add("attributes", key: ["attributes", sourceOwner!, member.Groups["name"].Value]);
            attributes.Add(aid, new() { Owner = owner, Name = member.Groups["name"].Value, TypeName = member.Groups["type"].Value, Keys = keys, Presentation = Presentation() });
        }
        if (owner is not null) throw new InvalidDataException("Unclosed entity block.");
        string Cardinality(string marker) => marker switch { "|o" or "o|" => "zeroOrOne", "||" => "one", "}o" or "o{" => "zeroOrMore", _ => "oneOrMore" };
        foreach (var line in links)
        {
            c.Line = line; var m = Match(line.Text, $"(?<from>{Id})\\s+(?<left>\\|o|\\|\\||\\}}o|\\}}\\|)(?<line>--|\\.\\.)(?<right>o\\||\\|\\||o\\{{|\\|\\{{)\\s+(?<to>{Id})\\s*:\\s*(?<label>.+)");
            if (!m.Success) throw new InvalidDataException("Unsupported ER relationship/cardinality.");
            relations.Add(c.Add("relations"), new()
            {
                From = c.Resolve("entities", m.Groups["from"].Value), To = c.Resolve("entities", m.Groups["to"].Value),
                Label = Text(m.Groups["label"].Value), FromCardinality = Cardinality(m.Groups["left"].Value),
                ToCardinality = Cardinality(m.Groups["right"].Value), Identifying = m.Groups["line"].Value == "--", Presentation = Presentation()
            });
        }
        return new() { Entities = entities, Attributes = attributes, Relations = relations };
    }

    private static C4Content2 C4(Context c, List<Line> lines, bool container)
    {
        var elements = new Dictionary<string, C4Element2>(); var boundaries = new Dictionary<string, C4Boundary2>();
        var relations = new Dictionary<string, C4Relation2>(); string? boundary = null;
        foreach (var line in lines)
        {
            c.Line = line;
            if (line.Text == "}") { if (boundary is null) throw new InvalidDataException("Unexpected C4 boundary end."); boundary = null; continue; }
            var m = Match(line.Text, @"(?<command>[A-Za-z_]+)\((?<args>.*)\)(?<block>\s*\{)?");
            if (!m.Success) throw new InvalidDataException("Unsupported C4 statement.");
            var args = Arguments(m.Groups["args"].Value); var command = m.Groups["command"].Value;
            string Identifier(int i)
            {
                if (i >= args.Count || !Match(args[i], Id).Success) throw new InvalidDataException("Expected C4 identifier.");
                return args[i];
            }
            string Quoted(int i, bool empty = false)
            {
                if (i >= args.Count || !args[i].StartsWith('"') || !args[i].EndsWith('"')) throw new InvalidDataException("Expected quoted C4 text.");
                return Text(args[i], empty);
            }
            if (command == "Container_Boundary")
            {
                if (!container || boundary is not null || boundaries.Count > 0 || args.Count != 2 || !m.Groups["block"].Success) throw new InvalidDataException("Unsupported C4 boundary/view/nesting.");
                boundary = c.Add("boundaries", Identifier(0)); boundaries.Add(boundary, new() { Name = Quoted(1), Presentation = Presentation() }); continue;
            }
            if (m.Groups["block"].Success) throw new InvalidDataException("Unsupported C4 block.");
            if (command == "Rel")
            {
                if (args.Count is < 3 or > 4) throw new InvalidDataException("Unsupported C4 relation arguments/scope.");
                relations.Add(c.Add("relations"), new() { From = c.Resolve("elements", Identifier(0)), To = c.Resolve("elements", Identifier(1)), Label = Quoted(2), Technology = args.Count == 4 ? Quoted(3) : null, Presentation = Presentation() }); continue;
            }
            var internalElement = command is "Container" or "ContainerDb";
            var kind = command switch { "Person" => "person", "System" => "system", "System_Ext" => "externalSystem", "Container" => "container", "ContainerDb" => "database", _ => throw new InvalidDataException("Unsupported C4 command.") };
            if (internalElement && (!container || boundary is null) || !internalElement && boundary is not null) throw new InvalidDataException("Invalid C4 element scope.");
            if (args.Count < (internalElement ? 3 : 2) || args.Count > (internalElement ? 4 : 3)) throw new InvalidDataException("Unsupported C4 argument count.");
            elements.Add(c.Add("elements", Identifier(0)), new()
            {
                Name = Quoted(1), Kind = kind, Boundary = internalElement ? boundary : null,
                Technology = internalElement ? Quoted(2) : null,
                Description = args.Count == (internalElement ? 4 : 3) ? Quoted(args.Count - 1, true) : null,
                Presentation = Presentation()
            });
        }
        if (boundary is not null) throw new InvalidDataException("Unclosed C4 boundary.");
        return new() { View = container ? "container" : "context", Elements = elements, Boundaries = boundaries, Relations = relations };
    }

    private static List<string> Arguments(string text)
    {
        var result = new List<string>(); var quoted = false; var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\') throw new InvalidDataException("Unsupported C4 escape.");
            if (text[i] == '"') quoted = !quoted;
            if (text[i] == ',' && !quoted) { result.Add(text[start..i].Trim()); start = i + 1; }
        }
        if (quoted) throw new InvalidDataException("Unclosed C4 quote.");
        result.Add(text[start..].Trim()); return result;
    }
}
