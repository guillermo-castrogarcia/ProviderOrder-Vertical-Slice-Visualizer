namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

// A link back to the source that defines a symbol. Captured with an absolute file path at analysis
// time (when the Roslyn symbol is still in hand) and rewritten to a repo-relative, forward-slashed
// path in a post-pass before serialization — hence Path is mutable. StartLine/EndLine are 1-based.
// Combined with the once-per-file SourceRepository header this yields a commit-pinned GitHub URL:
//   {RemoteBaseUrl}/blob/{CommitSha}/{Path}#L{StartLine}-L{EndLine}
public sealed record SourceLocation
{
    public required string Path { get; set; }
    public required int StartLine { get; init; }
    public required int EndLine { get; init; }
}
