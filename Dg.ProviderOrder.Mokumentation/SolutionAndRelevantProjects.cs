namespace Dg.ProviderOrder.Mokumentation;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

public sealed record SolutionAndRelevantProjects(Solution Solution, IImmutableSet<Project> RelevantProjects)
{
    public IEnumerable<Project> Projects => Solution.Projects;
}