using Microsoft.CodeAnalysis.FindSymbols;

namespace Dg.ProviderOrder.Mokumentation;

using ArchitectureBricks;
using Microsoft.CodeAnalysis;

public sealed record Call(
    NamedTypeSymbol CallingSymbolContainingType,
    NamedMethodSymbol CallingSymbolMethod,
    NamedTypeSymbol CalledSymbol);

public sealed record SerializableCallStack(IReadOnlyList<Call> Calls);

public sealed record CallStack(IReadOnlyList<SymbolCallerInfo> Calls)
{
    public bool Equals(CallStack? other)
    {
        if (other is null)
        {
            return false;
        }

        return Calls.SequenceEqual(other.Calls, CustomComparer.Default);
    }

    public override int GetHashCode()
    {
        return Calls.Count;
    }

    public bool ContainsMethodAsCallingSymbol(NamedMethodSymbol method)
    {
        for (var i = 0; i < Calls.Count; ++i)
        {
            if (Calls[i].CallingSymbol is IMethodSymbol callStackMethod && callStackMethod.ToNamedMethodSymbol().Equals(method))
            {
                return true;
            }
        }

        return false;
    }

    private class CustomComparer : IEqualityComparer<SymbolCallerInfo>
    {
        public static readonly CustomComparer Default = new CustomComparer();
        public bool Equals(SymbolCallerInfo x, SymbolCallerInfo y)
        {
            return x.CallingSymbol.Equals(y.CallingSymbol, SymbolEqualityComparer.Default)
                && x.Locations.SequenceEqual(y.Locations)
                && x.CalledSymbol.Equals(y.CalledSymbol, SymbolEqualityComparer.Default)
                && x.IsDirect == y.IsDirect;
        }

        public int GetHashCode(SymbolCallerInfo obj)
        {
            return HashCode.Combine(obj.CallingSymbol, obj.Locations, obj.CalledSymbol, obj.IsDirect);
        }
    }
}