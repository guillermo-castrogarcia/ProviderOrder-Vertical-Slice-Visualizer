namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;


public sealed record NamedTypeSymbol(string Name, FullName FullName, SourceLocation? SourceLocation = null)
{
    public string ToDisplayString() => FullName.Value;

    // Identity is Name + FullName only. SourceLocation is deliberately excluded so instances stay equal
    // across call sites and remain stable as dictionary/HashSet keys and in DistinctBy (e.g.
    // VerticalSlice.PrimaryAdapterByIncomingPayload, the payload de-duplication in FindVerticalSlices).
    public bool Equals(NamedTypeSymbol? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name && FullName.Equals(other.FullName);
    }

    public override int GetHashCode() => HashCode.Combine(Name, FullName);
}
