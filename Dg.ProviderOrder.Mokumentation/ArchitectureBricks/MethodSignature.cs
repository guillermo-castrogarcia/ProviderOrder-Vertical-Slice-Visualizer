namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

public sealed record MethodSignature(NamedTypeSymbol? ReturnValue, IReadOnlyList<NamedTypeSymbol> Parameters)
{
    public bool Equals(MethodSignature? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ReturnValue == other.ReturnValue && Parameters.SequenceEqual(other.Parameters);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ReturnValue);
    }
}