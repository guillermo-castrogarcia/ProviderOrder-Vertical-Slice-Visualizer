namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

using System.Text.Json.Serialization;

public sealed record VerticalSlice(
    IReadOnlyList<PrimaryAdapter> PrimaryAdapters,
    PrimaryPort PrimaryPort,
    IReadOnlyList<NamedTypeSymbol> NServiceBusPayloads,
    IReadOnlyList<NamedTypeSymbol> KafkaPayloads,
    IReadOnlyList<SerializableWebApiServiceClientCall> WebApiServiceClientCalls)
{
    [JsonIgnore]
    public IReadOnlySet<NamedTypeSymbol> OutgoingPayloads => NServiceBusPayloads
        .Union(KafkaPayloads)
        .ToHashSet();

    [JsonIgnore]
    public string Id => PrimaryPort.FullName.Value;

    [JsonIgnore]
    public IReadOnlySet<NamedTypeSymbol> IncomingPayloads => PrimaryAdapters.Select(e => e.PayloadType).OfType<NamedTypeSymbol>().ToHashSet();

    [JsonIgnore]
    public IReadOnlyDictionary<NamedTypeSymbol, PrimaryAdapter> PrimaryAdapterByIncomingPayload => PrimaryAdapters.Where(e => e.PayloadType != null).ToDictionary(e => e.PayloadType!);

    [JsonIgnore]
    public FullName PrimaryPortProductFullName => PrimaryPort.FullName.OnlyProductNameTokens;


    public List<VerticalSliceCall> VerticalSliceCalls { get; } = new List<VerticalSliceCall>();

    public bool Equals(VerticalSlice? other) => Id.Equals(other?.Id);

    public override int GetHashCode() => Id.GetHashCode();
}