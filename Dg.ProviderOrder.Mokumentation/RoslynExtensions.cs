using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using InvocationExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax;

namespace Dg.ProviderOrder.Mokumentation;

public static class RoslynExtensions
{
    public static async IAsyncEnumerable<DocumentAndSyntaxTreeAndSemanticModel> GetDocumentsAsync(
        this IEnumerable<Project> projects)
    {
        foreach (var project in projects)
        {
            await foreach (var documentAndSyntaxTree in project.GetDocumentsAsync())
            {
                yield return documentAndSyntaxTree;
            }
        }
    }

    public static bool IsErpViewFrameworkClass(this ISymbol symbol)
    {
        return SourceCodeConstants.ErpViewFrameworkNamespaces.Any(t => symbol.ToDisplayString().StartsWith(t));
    }

    public static async IAsyncEnumerable<DocumentAndSyntaxTreeAndSemanticModel> GetDocumentsAsync(this Project project)
    {
        var compilation = await project.GetCompilationAsync();
        if (compilation is not null)
        {
            foreach (Document document in project.Documents)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree is null)
                {
                    continue;
                }

                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                if (semanticModel is null)
                {
                    continue;
                }

                yield return new DocumentAndSyntaxTreeAndSemanticModel(document, syntaxTree, semanticModel);
            }
        }
    }

    public static async IAsyncEnumerable<ISymbol> FindTopLevelCallersAsync(
        this Solution solution,
        ISymbol methodSymbol,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var callers = (await SymbolFinder.FindCallersAsync(methodSymbol, solution, cancellationToken)).ToList();
        if (!callers.Any())
        {
            yield return methodSymbol;
        }

        foreach (var caller in callers)
        {
            var callingSymbol = caller.CallingSymbol;

            // Recursively inspect the callers of the caller
            await foreach (var parentCaller in FindTopLevelCallersAsync(solution, callingSymbol, cancellationToken))
            {
                yield return parentCaller;
            }
        }
    }

    public static bool IsImplementedBy(this IMethodSymbol interfaceMethod, IMethodSymbol implementationMethod)
    {
        if (Object.ReferenceEquals(interfaceMethod, implementationMethod))
        {
            return false;
        }

        if (interfaceMethod.Name != implementationMethod.Name)
        {
            return false;
        }

        if (interfaceMethod.Parameters.Length != implementationMethod.Parameters.Length)
        {
            return false;
        }

        for (var i = 0; i < interfaceMethod.Parameters.Length; ++i)
        {
            if (interfaceMethod.Parameters[i].ToDisplayString() != implementationMethod.Parameters[i].ToDisplayString())
            {
                return false;
            }
        }

        return true;
    }

    public static SemanticModel? GetSemanticModelOrNull(this Compilation compilation, SyntaxTree syntaxTree)
    {
        try
        {
            return compilation.GetSemanticModel(syntaxTree);
        }
        catch
        {
            return null;
        }
    }

    public sealed record MethodInvocation(
        InvocationExpressionSyntax Invocation,
        Location Location);

    public static async IAsyncEnumerable<ITypeSymbol> GetInvocationArgumentsAsync(
        this SymbolCallerInfo callerInfo,
        Solution solution,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var invocations = await callerInfo.GetMethodInvocationAsync(cancellationToken)
            .ToListAsync(cancellationToken);

        foreach (var invocation in invocations.Where(i => i.Invocation.ArgumentList.Arguments.Any()))
        {
            // Resolve the semantic model from the document that actually owns this syntax tree. Call sites can live in a
            // different project than the analyzed symbol, and trees parsed with different options cannot be spliced into
            // an unrelated compilation (that throws "Inconsistent syntax tree features").
            var document = solution.GetDocument(invocation.Invocation.SyntaxTree);
            if (document is null)
            {
                continue;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel is null)
            {
                continue;
            }

            foreach (var argument in invocation.Invocation.ArgumentList.Arguments)
            {
                var argumentTypeInfo = semanticModel.GetTypeInfo(argument.Expression, cancellationToken);
                if (argumentTypeInfo.Type != null)
                {
                    yield return argumentTypeInfo.Type;
                }
            }
        }
    }

    public static async IAsyncEnumerable<MethodInvocation> GetMethodInvocationAsync(
        this SymbolCallerInfo callerInfo,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        async Task<MethodInvocation?> GetMethodInvocation(Location location)
        {
            var invocationSyntax =
                await location.FindFirstParentOfTypeAsync<InvocationExpressionSyntax>(cancellationToken);

            if (invocationSyntax != null)
            {
                return new MethodInvocation(invocationSyntax!, location);
            }

            return null;
        }

        var invocations = await Task.WhenAll(callerInfo.Locations.Select(GetMethodInvocation));
        foreach (var invocation in invocations)
        {
            if (invocation is not null)
            {
                yield return invocation;
            }
        }
    }

    public static async Task<TNode?> FindFirstParentOfTypeAsync<TNode>(
        this Location location,
        CancellationToken cancellationToken) where TNode : SyntaxNode
    {
        return await location.FindFirstParentOfTypeAsync<TNode>((_) => true, cancellationToken);
    }

    public static async Task<TNode?> FindFirstParentOfTypeAsync<TNode>(
        this Location location,
        Func<TNode, bool> predicate,
        CancellationToken cancellationToken) where TNode : SyntaxNode
    {
        var syntaxRoot = await location.SourceTree!.GetRootAsync(cancellationToken);
        var currentNode = (SyntaxNode?)syntaxRoot.FindNode(location.SourceSpan);

        while (currentNode != null && (currentNode is not TNode typedNode || !predicate(typedNode)))
        {
            currentNode = currentNode.Parent;
        }

        return currentNode as TNode;
    }

    public static FullName GetFullName(this ISymbol symbol) => FullName.FromValue(symbol.ToDisplayString());
}

public sealed record DocumentAndSyntaxTreeAndSemanticModel(
    Document Document,
    SyntaxTree SyntaxTree,
    SemanticModel SemanticModel);