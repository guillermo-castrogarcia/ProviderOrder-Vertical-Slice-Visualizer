namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

public sealed record KafkaMessageHandler(
    NamedTypeSymbol MessageHandlerImplementation,
    NamedMethodSymbol ImplementationMethod,
    bool IsBatchMessageHandler,
    NamedTypeSymbol PayloadType);

