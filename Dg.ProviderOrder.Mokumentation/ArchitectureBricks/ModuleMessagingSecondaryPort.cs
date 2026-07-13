namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

using Microsoft.CodeAnalysis;

public sealed record ModuleMessagingSecondaryPort(
    MediatrRequestHandler CallingPrimaryPort,
    NamedTypeSymbol PassedArgument)
{
    public FullName FullName => PassedArgument.FullName;

    public string Name => FullName.Tokens.Last();
}