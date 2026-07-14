namespace Dg.ProviderOrder.Mockumentation.Viewer.Model;

/// <summary>A vertical slice, rendered as an ellipse node.</summary>
public sealed record GraphNodeDto(
    string Id,
    string Label,
    string Side,
    string Product,
    /// <summary>Input this slice receives that does NOT originate from another known slice — drawn as
    /// free-standing incoming arrows, one per (adapter type, payload).</summary>
    IReadOnlyList<ExternalIncomingDto> ExternalIncoming,
    /// <summary>Colour category for the node: the single adapter type of all its primary adapters
    /// ("Web"/"NServiceBus"/"Kafka"/"Other"), or "Mixed" when it has adapters of several types.</summary>
    string AdapterCategory);

/// <summary>A free-standing incoming arrow: external input into a slice from outside any known slice.</summary>
public sealed record ExternalIncomingDto(
    /// <summary>Adapter type ("Web"/"NServiceBus"/"Kafka"/"Other") — drives the arrow colour.</summary>
    string AdapterType,
    /// <summary>Name shown on hover: the message payload type for messaging adapters, or the entry
    /// method (controller action) for web adapters.</summary>
    string Payload);

/// <summary>A connection between two slices: an event/web request produced by one and consumed by another.</summary>
public sealed record GraphEdgeDto(
    string Id,
    string Source,
    string Target,
    /// <summary>Adapter type of the consuming end ("Web"/"Messaging"/"Other") — drives the arrow colour.</summary>
    string AdapterType,
    /// <summary>"Messaging" (NServiceBus/Kafka event) or "Web" (service-client request).</summary>
    string Kind,
    /// <summary>Simple name of the payload travelling along the arrow (the event/command type, or the
    /// web method for web edges) — shown on hover.</summary>
    string Payload,
    /// <summary>Commit-pinned GitHub URL to the payload type's source definition; null when the type has
    /// no known source (e.g. a type from a referenced package) — the arrow is then a plain hover target.</summary>
    string? SourceUrl = null);

public sealed record GraphDto(
    IReadOnlyList<GraphNodeDto> Nodes,
    IReadOnlyList<GraphEdgeDto> Edges);
