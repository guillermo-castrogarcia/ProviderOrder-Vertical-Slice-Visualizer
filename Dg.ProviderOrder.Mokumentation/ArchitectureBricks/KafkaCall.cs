namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;



public sealed record KafkaCall(
    CallStack CallStack,
    NamedTypeSymbol PayloadType);