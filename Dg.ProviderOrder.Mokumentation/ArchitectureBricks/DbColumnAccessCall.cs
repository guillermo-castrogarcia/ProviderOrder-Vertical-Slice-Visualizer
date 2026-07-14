namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

// Intermediate (never serialized) result of the DB-access finder: all the column accesses that occur inside one method,
// together with that method's reverse call stacks. A slice claims these accesses when its primary-port handler method
// either IS the containing method (the access sits directly in the handler body) or appears as a calling symbol in one
// of the call stacks (the handler transitively reaches the repository/method that performs the access). Mirrors how
// NServiceBusCall / KafkaCall carry a CallStack for slice attribution, but grouped per method to avoid regenerating the
// (expensive) reverse call graph once per individual column access.
public sealed record DbColumnAccessCall(
    NamedMethodSymbol ContainingMethod,
    IReadOnlyList<CallStack> CallStacks,
    IReadOnlyList<DbColumnAccess> Accesses);
