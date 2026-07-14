namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

using System.Text.Json.Serialization;

// A single database column touched by a vertical slice, fully qualified by its database, schema and table, plus whether
// the slice reads or writes it. This is the persisted shape (a positional record, so all five members serialize into
// *.verticalslices.json). Value equality across all members means the same column read AND written yields two distinct
// entries (different AccessKind), which is intentional.
public sealed record DbColumnAccess(
    string Database,
    string Schema,
    string Table,
    string Column,
    DbAccessKind AccessKind)
{
    [JsonIgnore]
    public string FullName => $"{Database}.{Schema}.{Table}.{Column}";
}
