namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

using Microsoft.CodeAnalysis;

public sealed record WebApiServiceClientCall(
    NamedMethodSymbol Method,
    CallStack CallStack,
    MethodSignature Signature)
{
    public SerializableWebApiServiceClientCall ToSerializableWebApiServiceClientCall() => new SerializableWebApiServiceClientCall(
        Method,
        Signature);
}

public sealed record SerializableWebApiServiceClientCall(
    NamedMethodSymbol Method,
    MethodSignature Signature);