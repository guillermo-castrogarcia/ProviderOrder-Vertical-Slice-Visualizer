namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

// Whether a vertical slice reads a column (query/projection/filter) or writes it (insert/update). Serialized as an
// int (0 = Read, 1 = Write) by default System.Text.Json, consistent with the other enums (PrimaryAdapterType, ...).
public enum DbAccessKind
{
    Read,
    Write,
}
