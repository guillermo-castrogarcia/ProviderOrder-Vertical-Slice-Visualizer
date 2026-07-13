namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;


public sealed record NamedMethodSymbol(
    string Name,
    FullName FullName,
    MethodSignature Signature)
{
    public string FullNameValue => FullName.Value;

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
