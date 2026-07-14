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
    string AdapterCategory,
    /// <summary>The slice's internals, shown when the node is expanded: incoming adapters, handling
    /// service, DB reads/writes, and outgoing messages/requests, each with a commit-pinned source link
    /// where one exists.</summary>
    SliceDetailDto Detail);

/// <summary>The internals of a slice, rendered inside the expanded node.</summary>
public sealed record SliceDetailDto(
    IReadOnlyList<AdapterDto> Adapters,
    HandlerDto? Handler,
    /// <summary>The MediatR/Marinator request/command the handler receives (derived from the handler's
    /// first parameter); null for non-mediated ports.</summary>
    LinkDto? MediatorPayload,
    IReadOnlyList<DbAccessDto> Reads,
    IReadOnlyList<DbAccessDto> Writes,
    /// <summary>Outgoing NServiceBus events/commands published by the slice (name + source link).</summary>
    IReadOnlyList<LinkDto> NsbOut,
    /// <summary>Outgoing Kafka events published by the slice (name; no link yet — a topic in the future).</summary>
    IReadOnlyList<LinkDto> KafkaOut,
    /// <summary>Outgoing web-service-client calls — the generated "{Controller}_{Action}" method names.</summary>
    IReadOnlyList<string> WebRequests);

/// <summary>An incoming primary adapter: a Kafka consumer, NSB receiver, or controller action.</summary>
public sealed record AdapterDto(
    /// <summary>"Web"/"NServiceBus"/"Kafka"/"Other" — the UI maps Web to "Controller Action".</summary>
    string AdapterType,
    string ClassName,
    string? ClassUrl,
    /// <summary>The entry method (controller action / handler method) name.</summary>
    string EntryMethod,
    string? EntryMethodUrl);

/// <summary>The handling service (the class implementing the primary port) and the handler method it runs.</summary>
public sealed record HandlerDto(
    string ClassName,
    string? ClassUrl,
    string MethodName,
    string? MethodUrl);

/// <summary>A named thing with an optional commit-pinned source link.</summary>
public sealed record LinkDto(string Name, string? Url);

/// <summary>A single database column read or written by the slice.</summary>
public sealed record DbAccessDto(string Database, string Schema, string Table, string Column);

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
