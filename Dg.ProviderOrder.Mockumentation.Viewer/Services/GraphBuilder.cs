using System.Text.Json;
using Dg.ProviderOrder.Mockumentation.Viewer.Model;

namespace Dg.ProviderOrder.Mockumentation.Viewer.Services;

public sealed class GraphBuilder(ILogger<GraphBuilder> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Loads the given *.verticalslices.json files, merges them (deduping by primary-port id so a
    /// slice present in both the monolith and module datasets appears once), and derives the graph:
    /// inter-slice edges from messaging events and web-service-client requests, plus free-standing
    /// "external incoming" arrows for input that does not originate from a known slice.
    /// </summary>
    public GraphDto Build(IEnumerable<string> filePaths)
    {
        var (slices, repoBySliceId) = LoadAndMerge(filePaths);
        var byId = slices.ToDictionary(s => s.Id);

        // payload full name -> commit-pinned GitHub URL of the type's source definition. A payload's
        // SourceLocation path is relative to the repository the slice was extracted from, so it must be
        // paired with that slice's repository. Producers (which publish the message) are scanned first so
        // their definition wins over a consumer's view of the same type.
        var sourceUrlByPayload = BuildPayloadSourceUrls(slices, repoBySliceId);

        // payload full name -> ids of slices that produce (send) it downstream.
        var producersByPayload = new Dictionary<string, HashSet<string>>();
        foreach (var slice in slices)
        {
            foreach (var payload in OutgoingPayloads(slice))
            {
                if (!producersByPayload.TryGetValue(payload, out var set))
                {
                    producersByPayload[payload] = set = [];
                }
                set.Add(slice.Id);
            }
        }

        var webAdapterIndex = BuildWebAdapterIndex(slices);

        var edges = new List<GraphEdgeDto>();
        var edgeKeys = new HashSet<string>();
        // Per slice, which of its incoming adapters are satisfied by a known upstream slice.
        var fedAdapters = new HashSet<PrimaryAdapterDto>(ReferenceEqualityComparer.Instance);

        void AddEdge(string source, string target, string adapterType, string kind, string discriminator, string? sourceUrl = null)
        {
            if (source == target)
            {
                return; // no self loops
            }
            var key = $"{source}{target}{kind}{discriminator}";
            if (edgeKeys.Add(key))
            {
                edges.Add(new GraphEdgeDto($"e{edges.Count}", source, target, adapterType, kind, SimpleName(discriminator), sourceUrl));
            }
        }

        // 1) Messaging / other edges: consumer adapter payload matches an upstream producer's outgoing payload.
        foreach (var consumer in slices)
        {
            foreach (var adapter in consumer.PrimaryAdapters)
            {
                var payload = adapter.PayloadType?.FullName.Value;
                if (payload is null)
                {
                    continue; // web adapters handled below
                }
                if (producersByPayload.TryGetValue(payload, out var producers))
                {
                    sourceUrlByPayload.TryGetValue(payload, out var payloadUrl);
                    foreach (var producerId in producers.Where(id => id != consumer.Id))
                    {
                        AddEdge(producerId, consumer.Id, ProviderOrderDomain.AdapterTypeName(adapter.AdapterType), "Messaging", payload, payloadUrl);
                        fedAdapters.Add(adapter);
                    }
                }
            }
        }

        // 2) Web edges: an outgoing web-service-client call resolves to another slice's web controller.
        var webMatched = 0;
        var webUnmatched = 0;
        foreach (var caller in slices)
        {
            foreach (var call in caller.WebApiServiceClientCalls)
            {
                var targets = ResolveWebCall(call, webAdapterIndex);
                if (targets.Count == 0)
                {
                    webUnmatched++;
                    continue;
                }
                webMatched++;
                foreach (var (targetSlice, adapter) in targets)
                {
                    AddEdge(caller.Id, targetSlice.Id, "Web", "Web", call.Method.FullName.Value);
                    fedAdapters.Add(adapter);
                }
            }
        }

        // 3) External incoming: any adapter not fed by a known slice is an external entry point.
        var nodes = new List<GraphNodeDto>(slices.Count);
        foreach (var slice in slices)
        {
            var external = new List<ExternalIncomingDto>();
            var seen = new HashSet<string>();
            foreach (var adapter in slice.PrimaryAdapters)
            {
                if (fedAdapters.Contains(adapter))
                {
                    continue;
                }
                var typeName = ProviderOrderDomain.AdapterTypeName(adapter.AdapterType);
                // Messaging adapters carry the incoming message type; web adapters carry no payload, so
                // fall back to the entry method (the controller action being called from outside).
                var payload = adapter.PayloadType is not null
                    ? SimpleName(adapter.PayloadType.FullName.Value)
                    : adapter.EntryMethod.Name;
                if (seen.Add($"{typeName}|{payload}"))
                {
                    external.Add(new ExternalIncomingDto(typeName, payload));
                }
            }

            var fullName = slice.PrimaryPort.FullName;
            nodes.Add(new GraphNodeDto(
                Id: slice.Id,
                Label: HandledCommandLabel(slice.PrimaryPort) ?? fullName.LastToken,
                Side: ProviderOrderDomain.SideName(slice.PrimaryPort.ApplicationSide),
                Product: ProviderOrderDomain.FindProduct(fullName.Value),
                ExternalIncoming: external,
                AdapterCategory: AdapterCategory(slice)));
        }

        logger.LogInformation(
            "Built graph: {NodeCount} slices, {EdgeCount} inter-slice edges (web calls matched {WebMatched}, unmatched {WebUnmatched}).",
            nodes.Count, edges.Count, webMatched, webUnmatched);

        return new GraphDto(nodes, edges);
    }

    private (List<VerticalSliceDto> Slices, Dictionary<string, SourceRepositoryDto> RepoBySliceId) LoadAndMerge(
        IEnumerable<string> filePaths)
    {
        var merged = new Dictionary<string, VerticalSliceDto>();
        var repoBySliceId = new Dictionary<string, SourceRepositoryDto>();
        foreach (var path in filePaths)
        {
            if (!File.Exists(path))
            {
                logger.LogWarning("Vertical slice file not found, skipping: {Path}", path);
                continue;
            }

            List<VerticalSliceDto>? slices;
            SourceRepositoryDto? repository;
            try
            {
                var bytes = File.ReadAllBytes(path);
                // Some generated files are prefixed with stray bytes (e.g. a "/c" from a shell redirect
                // mishap) before the JSON. Skip anything before the first '[' or '{' so we stay robust.
                var start = FindJsonStart(bytes);
                if (start > 0)
                {
                    logger.LogWarning("Skipped {Count} junk byte(s) before JSON in {Path}", start, path);
                }
                var extract = JsonSerializer.Deserialize<VerticalSliceExtractDto>(bytes.AsSpan(start), JsonOptions);
                slices = extract?.Slices;
                repository = extract?.Repository;
                if (extract is not null)
                {
                    logger.LogInformation(
                        "{Path}: repository {Repo} @ commit {Commit} ({Branch})",
                        path,
                        extract.Repository.RemoteBaseUrl,
                        extract.Repository.CommitSha,
                        extract.Repository.BranchName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read/parse {Path}", path);
                continue;
            }

            if (slices is null)
            {
                continue;
            }

            foreach (var slice in slices)
            {
                // First occurrence wins; a slice in both datasets is deduped by primary-port id.
                if (merged.TryAdd(slice.Id, slice) && repository is not null)
                {
                    repoBySliceId[slice.Id] = repository;
                }
            }
            logger.LogInformation("Loaded {Count} slices from {Path}", slices.Count, path);
        }

        return (merged.Values.ToList(), repoBySliceId);
    }

    // Maps each payload type's full name to a commit-pinned GitHub URL for its source definition. A payload
    // symbol's SourceLocation path is repo-relative, so it is composed with the repository of the slice that
    // references it. Producer-published payloads (NServiceBus/Kafka outgoing) are scanned first so a
    // publisher's definition wins over a consumer's incoming-adapter view of the same type; the first
    // non-null source location per payload name is kept.
    private static Dictionary<string, string> BuildPayloadSourceUrls(
        IEnumerable<VerticalSliceDto> slices, IReadOnlyDictionary<string, SourceRepositoryDto> repoBySliceId)
    {
        var urls = new Dictionary<string, string>();
        var sliceList = slices as ICollection<VerticalSliceDto> ?? slices.ToList();

        void Record(VerticalSliceDto slice, NamedSymbolDto? payload)
        {
            if (payload?.SourceLocation is null)
            {
                return;
            }
            var fullName = payload.FullName.Value;
            if (urls.ContainsKey(fullName) || !repoBySliceId.TryGetValue(slice.Id, out var repo))
            {
                return;
            }
            urls[fullName] = repo.BlobUrl(payload.SourceLocation);
        }

        // Pass 1: producers (outgoing payloads) define the message contract — prefer their location.
        foreach (var slice in sliceList)
        {
            foreach (var payload in slice.NServiceBusPayloads.Concat(slice.KafkaPayloads))
            {
                Record(slice, payload);
            }
        }
        // Pass 2: consumers' incoming adapter payloads, as a fallback for types no producer published.
        foreach (var slice in sliceList)
        {
            foreach (var adapter in slice.PrimaryAdapters)
            {
                Record(slice, adapter.PayloadType);
            }
        }

        return urls;
    }

    private static int FindJsonStart(byte[] bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            if (b == (byte)'[' || b == (byte)'{')
            {
                return i;
            }
        }
        return 0;
    }

    // PrimaryPortType values, mirroring the analysis project's PrimaryPortType enum.
    private const int MediatrRequestHandler = 0;
    private const int MarinatorRequestHandler = 1;

    // For MediatR (module) and Marinator (monolith) request handlers, the slice is better identified by
    // the request/command it handles than by the handler class name. That command is the handler
    // method's first parameter; its serialized token looks like "DeleteInvoiceParsingRequest request",
    // so we take the leading type portion. Returns null for other port types (falls back to the class name).
    private static string? HandledCommandLabel(PrimaryPortDto port)
    {
        if (port.PrimaryPortType is not (MediatrRequestHandler or MarinatorRequestHandler))
        {
            return null;
        }

        var firstParameter = port.ImplementationMethod.Signature.Parameters.FirstOrDefault();
        if (firstParameter is null)
        {
            return null;
        }

        var commandType = firstParameter.FullName.LastToken.Split(' ')[0];
        return string.IsNullOrWhiteSpace(commandType) ? null : commandType;
    }

    // Colour category for a slice's node: the single adapter type shared by all its primary adapters,
    // or "Mixed" when the slice exposes adapters of several different types.
    private static string AdapterCategory(VerticalSliceDto slice)
    {
        var types = slice.PrimaryAdapters
            .Select(a => ProviderOrderDomain.AdapterTypeName(a.AdapterType))
            .Distinct()
            .ToList();
        return types.Count switch
        {
            0 => "Other",
            1 => types[0],
            _ => "Mixed",
        };
    }

    // Last dotted segment of a full name, e.g. "Ns.Foo.OrderPlaced" -> "OrderPlaced".
    private static string SimpleName(string fullName)
    {
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 && lastDot < fullName.Length - 1 ? fullName[(lastDot + 1)..] : fullName;
    }

    private static IEnumerable<string> OutgoingPayloads(VerticalSliceDto slice) =>
        slice.NServiceBusPayloads.Concat(slice.KafkaPayloads)
            .Select(p => p.FullName.Value)
            .Distinct();

    // ---- Web-service-client call resolution -------------------------------------------------
    // A generated service-client method name follows the "{Controller}_{Action}" convention, e.g.
    // "ManualEmailOrderExport_GetMailtoUriAsync". We match it to a web (controller) adapter whose
    // entry method equals the action and whose class name carries the controller/area prefix.

    private sealed record WebAdapterEntry(VerticalSliceDto Slice, PrimaryAdapterDto Adapter, string ControllerName, string NormalizedMethod);

    private static List<WebAdapterEntry> BuildWebAdapterIndex(IEnumerable<VerticalSliceDto> slices)
    {
        var index = new List<WebAdapterEntry>();
        foreach (var slice in slices)
        {
            foreach (var adapter in slice.PrimaryAdapters)
            {
                if (adapter.AdapterType != 0)
                {
                    continue; // Web only
                }
                index.Add(new WebAdapterEntry(
                    slice,
                    adapter,
                    adapter.ClassTypeSymbol.FullName.LastToken,
                    Normalize(adapter.EntryMethod.Name)));
            }
        }
        return index;
    }

    private static List<(VerticalSliceDto Slice, PrimaryAdapterDto Adapter)> ResolveWebCall(
        WebApiServiceClientCallDto call, List<WebAdapterEntry> index)
    {
        var methodName = call.Method.Name;
        var underscore = methodName.IndexOf('_');
        var area = underscore > 0 ? methodName[..underscore] : null;
        var action = underscore > 0 ? methodName[(underscore + 1)..] : methodName;
        var normalizedAction = Normalize(action);

        return index
            .Where(e => e.NormalizedMethod == normalizedAction
                        && (area is null || e.ControllerName.Contains(area, StringComparison.OrdinalIgnoreCase)))
            .Select(e => (e.Slice, e.Adapter))
            .ToList();
    }

    private static string Normalize(string methodName)
    {
        if (methodName.EndsWith("Async", StringComparison.Ordinal))
        {
            methodName = methodName[..^"Async".Length];
        }
        return methodName;
    }
}
