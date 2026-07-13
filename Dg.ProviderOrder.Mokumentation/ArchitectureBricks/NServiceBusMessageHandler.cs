namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

using Microsoft.CodeAnalysis;

public sealed record NServiceBusMessageHandler(
    NamedTypeSymbol MessageHandlerImplementation,
    NamedMethodSymbol ImplementationMethod,
    NamedTypeSymbol PayloadType);
