using System.Text.Json.Serialization;

namespace Sddp.Abstractions.Models.Diagrams.V2;

/// <summary>SDDP-owned typed semantic payload, independent of parser or renderer providers.</summary>
public abstract record DiagramContent2;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SequenceMessage2), "message")]
[JsonDerivedType(typeof(SequenceActivationStart2), "activationStart")]
[JsonDerivedType(typeof(SequenceActivationEnd2), "activationEnd")]
[JsonDerivedType(typeof(SequenceFragment2), "fragment")]
public abstract record SequenceEvent2;

/// <summary>Schema 2 common ElementRef contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ElementRef
{
    public required string Collection { get; init; }
    public required string Id { get; init; }
}

/// <summary>Schema 2 common DiagramPresentation2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DiagramPresentation2
{
    public required Dictionary<string, string> Properties
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required List<ElementRef> Order { get; init; }
}

/// <summary>Schema 2 common DiagramProfile2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DiagramProfile2
{
    public required string Id { get; init; }
    public required int Version { get; init; }
}

/// <summary>Schema 2 common DiagramAdapter2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DiagramAdapter2
{
    public required string Id { get; init; }
    public required string Version { get; init; }
}

/// <summary>Schema 2 common DiagramSourceRange2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DiagramSourceRange2
{
    public required int Start { get; init; }
    public required int End { get; init; }
}

/// <summary>Schema 2 common DiagramBinding2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DiagramBinding2
{
    public required ElementRef Element { get; init; }
    public required List<string>? Key { get; init; }
    public required DiagramSourceRange2? Range { get; init; }
}

/// <summary>Schema 2 common DiagramSourceMap2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DiagramSourceMap2
{
    public required string SourceHash { get; init; }
    public required DiagramProfile2 Profile { get; init; }
    public required DiagramAdapter2 Adapter { get; init; }
    public required List<DiagramBinding2> Bindings { get; init; }
}

/// <summary>Schema 2 common DiagramRevision2 contract.</summary>
[JsonConverter(typeof(DiagramRevision2Converter))]
public sealed record DiagramRevision2
{
    public required int SchemaVersion { get; init; }
    public required string DiagramId { get; init; }
    public required int Revision { get; init; }
    public required string DiagramType { get; init; }
    public required DiagramSource Source { get; init; }
    public required string SourceState { get; init; }
    public required DiagramSourceMap2? SourceMap { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
    public required DiagramContent2 Content { get; init; }
}

/// <summary>Schema 2 common GraphContent2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record GraphContent2 : DiagramContent2
{
    public required Dictionary<string, GraphNode2> Nodes
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, GraphEdge2> Edges
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
}

/// <summary>Schema 2 common GraphNode2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record GraphNode2
{
    public required string Label { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common GraphEdge2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record GraphEdge2
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Label { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common FlowchartContent2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FlowchartContent2 : DiagramContent2
{
    public required Dictionary<string, FlowNode2> Nodes
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, FlowEdge2> Edges
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
}

/// <summary>Schema 2 common FlowNode2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FlowNode2
{
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common FlowEdge2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FlowEdge2
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Label { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common SequenceContent2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SequenceContent2 : DiagramContent2
{
    public required Dictionary<string, SequenceParticipant2> Participants
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, SequenceEvent2> Events
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, SequenceBranch2> Branches
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required List<string> Timeline { get; init; }
}

/// <summary>Schema 2 common SequenceParticipant2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SequenceParticipant2
{
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common SequenceBranch2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SequenceBranch2
{
    public required string Kind { get; init; }
    public required string Label { get; init; }
    public required List<string> Events { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common SequenceMessage2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SequenceMessage2 : SequenceEvent2
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Kind { get; init; }
    public required string Text { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common SequenceActivationStart2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SequenceActivationStart2 : SequenceEvent2
{
    public required string Participant { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common SequenceActivationEnd2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SequenceActivationEnd2 : SequenceEvent2
{
    public required string Start { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common SequenceFragment2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SequenceFragment2 : SequenceEvent2
{
    public required string Kind { get; init; }
    public required List<string> BranchOrder { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common ClassContent2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ClassContent2 : DiagramContent2
{
    public required Dictionary<string, ClassType2> Types
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, ClassAttribute2> Attributes
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, ClassOperation2> Operations
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, ClassParameter2> Parameters
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, ClassRelation2> Relations
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
}

/// <summary>Schema 2 common ClassType2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ClassType2
{
    public required string Name { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common ClassAttribute2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ClassAttribute2
{
    public required string Owner { get; init; }
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required string Visibility { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common ClassOperation2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ClassOperation2
{
    public required string Owner { get; init; }
    public required string Name { get; init; }
    public required string Visibility { get; init; }
    public required string? ReturnType { get; init; }
    public required List<string> ParameterOrder { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common ClassParameter2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ClassParameter2
{
    public required string Name { get; init; }
    public required string? TypeName { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common ClassRelation2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ClassRelation2
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Kind { get; init; }
    public required string Label { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common StateContent2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record StateContent2 : DiagramContent2
{
    public required Dictionary<string, StateNode2> States
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, StateTransition2> Transitions
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
}

/// <summary>Schema 2 common StateNode2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record StateNode2
{
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common StateTransition2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record StateTransition2
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Event { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common ErContent2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ErContent2 : DiagramContent2
{
    public required Dictionary<string, ErEntity2> Entities
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, ErAttribute2> Attributes
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, ErRelation2> Relations
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
}

/// <summary>Schema 2 common ErEntity2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ErEntity2
{
    public required string Name { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common ErAttribute2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ErAttribute2
{
    public required string Owner { get; init; }
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required List<string> Keys { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common ErRelation2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ErRelation2
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Label { get; init; }
    public required string FromCardinality { get; init; }
    public required string ToCardinality { get; init; }
    public required bool Identifying { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common C4Content2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record C4Content2 : DiagramContent2
{
    public required string View { get; init; }
    public required Dictionary<string, C4Element2> Elements
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, C4Boundary2> Boundaries
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
    public required Dictionary<string, C4Relation2> Relations
    {
        get;
        init => field = DiagramKeys.Ordinal(value);
    }
}

/// <summary>Schema 2 common C4Element2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record C4Element2
{
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required string? Description { get; init; }
    public required string? Technology { get; init; }
    public required string? Boundary { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common C4Boundary2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record C4Boundary2
{
    public required string Name { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}

/// <summary>Schema 2 common C4Relation2 contract.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record C4Relation2
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Label { get; init; }
    public required string? Technology { get; init; }
    public required DiagramPresentation2 Presentation { get; init; }
}
