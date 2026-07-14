using LibGit2Sharp;

namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

// Builds the once-per-file SourceRepository header from the LibGit2Sharp repository being analyzed:
// the GitHub base URL (origin, without the trailing ".git"), the exact commit extracted from, and the
// branch. Source links are pinned to CommitSha so they never drift as the branch moves.
public static class GitHubSourceRepository
{
    public static SourceRepository? Create(Repository repository)
    {
        var originUrl = repository.Network.Remotes["origin"]?.Url;
        if (originUrl is null)
        {
            return null;
        }

        var remoteBaseUrl = originUrl.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? originUrl[..^".git".Length]
            : originUrl;

        return new SourceRepository(
            RemoteBaseUrl: remoteBaseUrl.TrimEnd('/'),
            CommitSha: repository.Head.Tip.Sha,
            BranchName: repository.Head.FriendlyName);
    }
}
