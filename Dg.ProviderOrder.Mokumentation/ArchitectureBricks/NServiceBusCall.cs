namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

using Microsoft.CodeAnalysis;

public sealed record NServiceBusCall(
    CallStack CallStack,
    NamedTypeSymbol PayloadType);