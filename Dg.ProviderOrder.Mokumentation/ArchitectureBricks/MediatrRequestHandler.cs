namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;


public sealed record MediatrRequestHandler(
    NamedTypeSymbol ImplementationClass,
    NamedMethodSymbol ImplementationMethod,
    NamedTypeSymbol RequestType,
    PrimaryPortType PrimaryPortType,
    VersionControlInfo? VersionControlInfo = null) : PrimaryPort(ImplementationClass, ImplementationMethod, PrimaryPortType, VersionControlInfo);
