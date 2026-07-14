namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

// The source repository a *.verticalslices.json was extracted from, held once per file. Together with
// each SourceLocation's relative Path it composes commit-pinned GitHub links, so the base URL and the
// exact commit are not repeated on every node.
public sealed record SourceRepository(string RemoteBaseUrl, string CommitSha, string BranchName);
