using Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

public sealed record VerticalSliceCall(VerticalSlice CalledVerticalSlice, PrimaryAdapter CalledPrimaryAdapter, NamedTypeSymbol Payload);