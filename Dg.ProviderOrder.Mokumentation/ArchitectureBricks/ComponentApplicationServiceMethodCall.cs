namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

using Microsoft.CodeAnalysis;

public sealed record ComponentApplicationServiceMethodCall(
    CallStack CallStack,
    PrimaryPort CalledMethod)
{
    public NamedMethodSymbol? CallStackTopMethod
    {
        get
        {
            var lastMethodSymbol = CallStack.Calls.Any()
                ? CallStack.Calls[^1].CallingSymbol as IMethodSymbol
                : null;

            return lastMethodSymbol?.ToNamedMethodSymbol();
        }
    }
}