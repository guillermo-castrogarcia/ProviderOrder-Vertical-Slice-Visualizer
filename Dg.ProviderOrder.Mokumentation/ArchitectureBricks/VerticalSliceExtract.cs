namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

// Top-level serialized shape of a *.verticalslices.json file: the source repository/commit header
// (held once) plus the extracted slices.
public sealed record VerticalSliceExtract(SourceRepository Repository, IReadOnlyList<VerticalSlice> Slices);
