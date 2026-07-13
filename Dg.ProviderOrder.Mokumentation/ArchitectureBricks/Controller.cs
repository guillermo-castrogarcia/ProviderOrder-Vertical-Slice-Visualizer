namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

using Microsoft.CodeAnalysis;

public record ControllerAction(
    NamedTypeSymbol ClassTypeSymbol,
    NamedMethodSymbol EntryMethod);