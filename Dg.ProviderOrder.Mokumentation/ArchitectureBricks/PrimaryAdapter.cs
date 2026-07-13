namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;


public record PrimaryAdapter(
    NamedTypeSymbol ClassTypeSymbol,
    NamedMethodSymbol EntryMethod,
    PrimaryPort CalledPort,
    PrimaryAdapterType AdapterType,
    NamedTypeSymbol? PayloadType)
{
    public FullName FullName => ClassTypeSymbol.FullName;
}


