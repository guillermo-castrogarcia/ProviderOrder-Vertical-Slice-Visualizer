namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;


public sealed record NamedMethodSymbol(
    string Name,
    FullName FullName,
    MethodSignature Signature,
    SourceLocation? SourceLocation = null)
{
    public string FullNameValue => FullName.Value;

    // Equals/GetHashCode below are hand-written over Name/FullName/Signature, so the added
    // SourceLocation is naturally excluded from identity — no change needed there.

    public bool Equals(NamedMethodSymbol? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name && FullName.Equals(other.FullName) && Signature.Equals(other.Signature);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, FullName);
    }
}
