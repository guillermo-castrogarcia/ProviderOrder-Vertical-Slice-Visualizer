// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;
using Dg.ProviderOrder.Mokumentation;
using Dg.ProviderOrder.Mokumentation.ArchitectureBricks;
using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal partial class Program
{
    private static async IAsyncEnumerable<PrimaryPort> FindMonolithComponentApplicationServicesAsync(
        Solution solution,
        Repository repository,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var project = solution.Projects.First(p => p.Name == SourceCodeConstants.ProviderOrderProjectName);
        foreach (var documentId in project.DocumentIds)
        {
            var document = project.GetDocument(documentId);
            if (document != null)
            {
                var relativePath = document.FilePath != null ? Path.GetRelativePath(repository.Info.Path, document.FilePath) : null;
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                if (root != null)
                {
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

                    var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                    foreach (var classDeclaration in classDeclarations)
                    {
                        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                        if (classSymbol != null && classSymbol.ContainingNamespace.ToDisplayString().StartsWith(SourceCodeConstants.ProviderOrderApplicationNamespace))
                        {
                            var interfaceDeclarationMethods = classSymbol.AllInterfaces.SelectMany(i => i.GetMembers().OfType<IMethodSymbol>()).ToList();
                            var publicMethods = classSymbol.GetMembers()
                                .OfType<IMethodSymbol>()
                                .Where(m => m.DeclaredAccessibility == Accessibility.Public)
                                .ToList();

                            foreach (var method in publicMethods)
                            {
                                foreach (var explicitInterfaceImplementation in method.ExplicitInterfaceImplementations)
                                {
                                    yield return new PrimaryPort(
                                        classSymbol.ToNamedTypeSymbol(),
                                        method.ToNamedMethodSymbol(),
                                        PrimaryPortType.MonolithApplicationService);
                                }

                                foreach (var implicitInterfaceImplementation in interfaceDeclarationMethods.Where(m => m.IsImplementedBy(method)))
                                {
                                    yield return new PrimaryPort(
                                        classSymbol.ToNamedTypeSymbol(),
                                        method.ToNamedMethodSymbol(),
                                        PrimaryPortType.MonolithApplicationService);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static async IAsyncEnumerable<ComponentApplicationServiceMethodCall> FindComponentApplicationServiceMethodCallsAsync(
        Solution solution,
        IReadOnlyList<PrimaryPort> primaryPorts,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var primaryPort in primaryPorts)
        {
            var callStacks = await primaryPort.GenerateCallStacksAsync(solution, cancellationToken).ToListAsync(cancellationToken);
            foreach (var callStack in callStacks.Where(c => c.Calls[0].CallingSymbol.ToDisplayString().Contains(".Infrastructure.")))
            {
                yield return new ComponentApplicationServiceMethodCall(
                    callStack,
                    primaryPort);
            }
        }
    }


}