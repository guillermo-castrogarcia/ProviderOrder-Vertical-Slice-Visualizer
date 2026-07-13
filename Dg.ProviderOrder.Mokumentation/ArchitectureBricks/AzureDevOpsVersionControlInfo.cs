using LibGit2Sharp;
using Microsoft.CodeAnalysis;

namespace Dg.ProviderOrder.Mokumentation.ArchitectureBricks;

public sealed record VersionControlInfo(string RepositoryPath, string BranchName, string LinkToRemoteFile);

public static class AzureDevOpsVersionControlInfo
{
    public static VersionControlInfo? Create(Document document, Repository repository)
    {
        if (document.FilePath is null)
        {
            return null;
        }

        var originUrl = repository.Network.Remotes["origin"]?.Url;
        if (originUrl is null)
        {
            return null;
        }

        var repositoryPath = Path.GetRelativePath($"{repository.Info.Path}../", document.FilePath);
        var branchName = repository.Head.FriendlyName;
        var linkToRemoteFile = $"{originUrl}?path={Uri.EscapeDataString(repositoryPath)}&version=GB{Uri.EscapeDataString(repository.Head.FriendlyName)}";
        return new VersionControlInfo(RepositoryPath: repositoryPath, BranchName: branchName, LinkToRemoteFile: linkToRemoteFile);
    }
}