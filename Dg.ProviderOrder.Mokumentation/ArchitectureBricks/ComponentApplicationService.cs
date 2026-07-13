namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

using Microsoft.CodeAnalysis;

public record PrimaryPort(
    NamedTypeSymbol ImplementationClass,
    NamedMethodSymbol ImplementationMethod,
    PrimaryPortType PrimaryPortType,
    VersionControlInfo? VersionControlInfo = null)
{
    public FullName FullName => this.ImplementationClass.FullName;

    public ApplicationSide ApplicationSide => FullName.Value.Contains(".Commands.") ? ApplicationSide.Command : ApplicationSide.Query;
}