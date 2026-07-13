namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;


public sealed record NamedTypeSymbol(string Name, FullName FullName)
{
    public string ToDisplayString() => FullName.Value;
}