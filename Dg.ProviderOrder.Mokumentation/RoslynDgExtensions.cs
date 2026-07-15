using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using InvocationExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax;

namespace Dg.ProviderOrder.Mokumentation;

using ArchitectureBricks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Schema;

public static class RoslynDgExtensions
{
    public static bool IsController(this INamedTypeSymbol symbol)
    {
        return symbol.BaseType != null && SourceCodeConstants.AllControllerBaseClassFullNames.Any(t => t == symbol.BaseType.ToDisplayString());
    }

    public static NamedTypeSymbol ToNamedTypeSymbol(this IParameterSymbol symbol) =>
        new(
            symbol.Name,
            FullName.FromValue(symbol.ToDisplayString()),
            // Link to the parameter's type declaration, not the parameter itself.
            symbol.Type.ToSourceLocation());

    public static NamedTypeSymbol ToNamedTypeSymbol(this ITypeSymbol symbol) =>
        new(
            symbol.Name,
            FullName.FromValue(symbol.ToDisplayString()),
            symbol.ToSourceLocation());

    public static NamedMethodSymbol ToNamedMethodSymbol(this IMethodSymbol symbol) => new(
        Name: symbol.Name,
        FullName: FullName.FromValue(
            symbol.ToDisplayString(
                new SymbolDisplayFormat(
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                    memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes))),
        Signature: new MethodSignature(
            ReturnValue: symbol.ReturnsVoid ? null : symbol.ReturnType.ToNamedTypeSymbol(),
            Parameters: symbol.Parameters.Select(ToNamedTypeSymbol).ToList()),
        SourceLocation: symbol.ToSourceLocation());

    // Resolves the source file + line range that declares a symbol, as an ABSOLUTE path (the path is
    // rewritten to repo-relative in a post-pass once the repo root is known). Returns null for symbols
    // with no source declaration (e.g. types defined in referenced assemblies / NuGet packages), so
    // those simply carry no link. Lines are 1-based to match GitHub's #L anchors.
    public static SourceLocation? ToSourceLocation(this ISymbol symbol)
    {
        var location = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()
            ?? symbol.Locations.FirstOrDefault(l => l.IsInSource);
        if (location is null || !location.IsInSource)
        {
            return null;
        }

        var lineSpan = location.GetLineSpan();
        return new SourceLocation
        {
            Path = lineSpan.Path,
            StartLine = lineSpan.StartLinePosition.Line + 1,
            EndLine = lineSpan.EndLinePosition.Line + 1,
        };
    }

    public static async Task<Document?> FindDocumentAsync(this IMethodSymbol methodSymbol, Solution solution, CancellationToken cancellationToken = default)
    {
        var syntaxReferences = methodSymbol.DeclaringSyntaxReferences;

        foreach (var syntaxReference in syntaxReferences)
        {
            // Get the syntax node from the reference
            var syntaxNode = await syntaxReference.GetSyntaxAsync(cancellationToken);

            // Find the document containing the syntax node
            foreach (var document in solution.Projects.SelectMany(p => p.Documents))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                if(root != null && root.Contains(syntaxNode))
                {
                    return document;
                }
            }
        }

        return null;
    }
    // MediatR (module) request handlers. See FindRequestHandlersAsync for the shared implementation.
    public static IAsyncEnumerable<MediatrRequestHandler> FindMediatrRequestHandlersAsync(
        this Solution solution,
        CancellationToken cancellationToken) =>
        solution.FindRequestHandlersAsync(
            MediatorConstants.RequestHandlerInterfaceFullNamesithArity,
            MediatorConstants.RequestHandlerInterfaceName,
            MediatorConstants.RequestHandlerMethodName,
            PrimaryPortType.MediatrRequestHandler,
            cancellationToken);

    // Marinator (monolith) command handlers. Marinator is MediatR-shaped, so this reuses the exact same detection.
    public static IAsyncEnumerable<MediatrRequestHandler> FindMarinatorRequestHandlersAsync(
        this Solution solution,
        CancellationToken cancellationToken) =>
        solution.FindRequestHandlersAsync(
            MarinatorConstants.RequestHandlerInterfaceFullNamesWithArity,
            MarinatorConstants.RequestHandlerInterfaceName,
            MarinatorConstants.RequestHandlerMethodName,
            PrimaryPortType.MarinatorRequestHandler,
            cancellationToken);

    // Finds every class implementing a MediatR/Marinator-style request-handler interface and turns each handled
    // request type into a PrimaryPort. Interface types are resolved across all projects (not just the first) so this
    // works regardless of which project defines/references the framework.
    private static async IAsyncEnumerable<MediatrRequestHandler> FindRequestHandlersAsync(
        this Solution solution,
        string[] requestHandlerInterfaceFullNames,
        string requestHandlerInterfaceName,
        string requestHandlerMethodName,
        PrimaryPortType primaryPortType,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var interfaceTypes = new List<INamedTypeSymbol>();
        foreach (var interfaceFullName in requestHandlerInterfaceFullNames)
        {
            var interfaceType = await solution.GetTypeByMetadataNameAsync(interfaceFullName, cancellationToken);
            if (interfaceType is not null)
            {
                interfaceTypes.Add(interfaceType);
            }
        }

        var implementations = (await Task.WhenAll(
                interfaceTypes
                    .Select(
                        it =>
                            SymbolFinder.FindImplementationsAsync(
                                type: it,
                                solution: solution,
                                cancellationToken: cancellationToken))))
            .SelectMany(e => e)
            .ToList();

        foreach (var implementation in implementations)
        {
            var requestHandlerInterfaces = implementation.Interfaces
                .Where(i => i.Name == requestHandlerInterfaceName)
                .ToList();

            foreach (var requestHandlerInterface in requestHandlerInterfaces)
            {
                var requestType = requestHandlerInterface.TypeArguments.First();
                var handlerMethod = implementation
                    .GetMembers()
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(
                        e => e.Name == requestHandlerMethodName
                            && requestType.Equals(
                                e.Parameters.FirstOrDefault()?.Type,
                                SymbolEqualityComparer.Default));

                if (handlerMethod != null)
                {
                    // The source link for the port now rides on the enriched ImplementationClass /
                    // ImplementationMethod wrappers (see ToNamedTypeSymbol/ToNamedMethodSymbol).
                    yield return new MediatrRequestHandler(
                        implementation.ToNamedTypeSymbol(),
                        handlerMethod.ToNamedMethodSymbol(),
                        handlerMethod.Parameters[0].Type.ToNamedTypeSymbol(),
                        PrimaryPortType: primaryPortType);
                }
            }
        }
    }

    // MediatR (module) dispatch calls: ISender.Send(request). See FindDispatchCallsAsync for the shared implementation.
    public static IAsyncEnumerable<MediatorCall> FindMediatorCallsAsync(
        this Solution solution,
        CancellationToken cancellationToken) =>
        solution.FindDispatchCallsAsync(MediatorConstants.SenderInterfaceFullName, cancellationToken);

    // Marinator (monolith) dispatch calls: IMarinator.SendAsync(command, ...).
    public static IAsyncEnumerable<MediatorCall> FindMarinatorCallsAsync(
        this Solution solution,
        CancellationToken cancellationToken) =>
        solution.FindDispatchCallsAsync(MarinatorConstants.DispatcherInterfaceFullName, cancellationToken);

    // Finds every call to a dispatcher interface's method(s) and records the request/command type passed as the first
    // argument, together with the call stack up to the entry point.
    private static async IAsyncEnumerable<MediatorCall> FindDispatchCallsAsync(
        this Solution solution,
        string dispatcherInterfaceFullName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var interfaceType = await solution.GetTypeByMetadataNameAsync(dispatcherInterfaceFullName, cancellationToken);
        if (interfaceType is null)
        {
            yield break;
        }

        var methods = interfaceType
            .GetMembers()
            .OfType<IMethodSymbol>()
            .ToList();

        foreach (var method in methods)
        {
            var callStacks = await method.GenerateCallStacksAsync(solution, DefaultCallerFilter, AnyStackIsValidFilter.Default, cancellationToken).ToListAsync(cancellationToken);
            foreach (var callStack in callStacks)
            {
                var nonGenericCalls = callStack.Calls.SkipWhile(c => (c.CallingSymbol as IMethodSymbol)?.IsGenericMethod == true).ToList();
                if (nonGenericCalls.Any())
                {
                    var firstNonGenericCall = nonGenericCalls.First();
                    var arguments = await firstNonGenericCall.GetInvocationArgumentsAsync(solution, cancellationToken).ToListAsync(cancellationToken);
                    if (arguments.FirstOrDefault() is INamedTypeSymbol requestType && requestType.ToDisplayString() != "?")
                    {
                        yield return new MediatorCall(
                            callStack,
                            requestType.ToNamedTypeSymbol());
                    }
                }
            }
        }
    }

    public static async IAsyncEnumerable<CallStack> GenerateCallStacksAsync(
        this IMethodSymbol method,
        Solution solution,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var callstack in GenerateCallStacksAsync(method, solution, DefaultCallerFilter, AnyStackIsValidFilter.Default, cancellationToken))
        {
            yield return callstack;
        }
    }

    public static async IAsyncEnumerable<CallStack> GenerateCallStacksAsync(
        this PrimaryPort primaryPort,
        Solution solution,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var containerType = await solution.GetTypeByMetadataNameAsync(primaryPort.ImplementationClass.FullName.Value, cancellationToken);
        if (containerType is null)
        {
            yield break;
        }

        var methodSymbolTuple = containerType!
            .GetMembers(primaryPort.ImplementationMethod.Name)
            .OfType<IMethodSymbol>()
            .Select(e => new { MethodSymbol = e, NamedMethodSymbol = e.ToNamedMethodSymbol()})
            .FirstOrDefault(e => e.NamedMethodSymbol.Equals(primaryPort.ImplementationMethod));

        if (methodSymbolTuple is null)
        {
            yield break;
        }

        await foreach (var callstack in methodSymbolTuple.MethodSymbol.GenerateCallStacksAsync(solution, cancellationToken))
        {
            yield return callstack;
        }
    }

    public static async IAsyncEnumerable<CallStack> GenerateCallStacksAsync(
        this IMethodSymbol method,
        Solution solution,
        Func<SymbolCallerInfo, bool> callerFilter,
        ICallStackFilter stackFilter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var result in method.GenerateCallStacksRecursiveAsync(
                           solution,
                           new List<SymbolCallerInfo>(),
                           callerFilter,
                           stackFilter,
                           cancellationToken))
        {
            yield return result;
        }
    }

    public static bool IsWorkflow(this ISymbol symbol)
    {
        return symbol.ToDisplayString().StartsWith(SourceCodeConstants.LogisticsWorkflowManagerClassName)
            || symbol.ToDisplayString().StartsWith(SourceCodeConstants.PortalWorkflowMNamespace);
    }

    public static bool IsModificationStrategyManagerCall(this ISymbol symbol)
    {
        return symbol.ToDisplayString().StartsWith(SourceCodeConstants.OrderPositionModificationStrategyManagerProcessMethodFullName)
            || symbol.ToDisplayString().StartsWith(SourceCodeConstants.OrderModificationStrategyManagerProcessMethodFullName);
    }

    public static async IAsyncEnumerable<ControllerAction> FindControllerActionsAsync(
        this Solution solution,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var projectId in solution.ProjectIds)
        {
            var project = solution.GetProject(projectId);
            if (project is null)
            {
                continue;
            }

            foreach (var documentId in project.DocumentIds)
            {
                var document = project.GetDocument(documentId);
                if (document is null)
                {
                    continue;
                }

                var root = await document.GetSyntaxRootAsync(cancellationToken);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

                if (root is null)
                {
                    continue;
                }

                var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDeclaration in classDeclarations)
                {
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
                    if (classSymbol is null)
                    {
                        continue;
                    }

                    if (classSymbol.IsController())
                    {
                        foreach (var method in classSymbol
                                     .GetMembers()
                                     .OfType<IMethodSymbol>()
                                     .Where(m => m.DeclaredAccessibility == Accessibility.Public))

                        {
                            yield return new ControllerAction(
                                classSymbol.ToNamedTypeSymbol(),
                                method.ToNamedMethodSymbol());
                        }
                    }
                }
            }
        }
    }

    public static async IAsyncEnumerable<NServiceBusMessageHandler> FindNServiceBusMessageHandlersAsync(
        this Solution solution,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var interfaceType = await solution.GetTypeByMetadataNameAsync(NServiceBusConstants.MessageHandlerInterfaceFullNameWithArity, cancellationToken);
        if (interfaceType is null)
        {
            yield break;
        }

        var implementations = await SymbolFinder.FindImplementationsAsync(
            type: interfaceType!,
            solution: solution,
            cancellationToken: cancellationToken);

        foreach (var implementation in implementations)
        {
            var nServiceBusReceiverInterfaces = implementation.Interfaces.Where(i => i.Name == NServiceBusConstants.MessageHandlerInterfaceName)
                .ToList();

            foreach (var nServiceBusReceiverInterface in nServiceBusReceiverInterfaces)
            {
                var payloadType = nServiceBusReceiverInterface.TypeArguments.First();
                var handlerMethod = implementation
                    .GetMembers()
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(
                        e => e.Name == NServiceBusConstants.MessageHandlerMethodName
                            && payloadType.Equals(
                                e.Parameters.FirstOrDefault()?.Type,
                                SymbolEqualityComparer.Default));

                if (handlerMethod != null)
                {
                    yield return new NServiceBusMessageHandler(
                        implementation.ToNamedTypeSymbol(),
                        handlerMethod!.ToNamedMethodSymbol(),
                        handlerMethod.Parameters[0].Type.ToNamedTypeSymbol());
                }
            }
        }
    }

    private static async Task<INamedTypeSymbol?> GetTypeByMetadataNameAsync(
        this Solution solution,
        string fullyQualifiedMetadataName,
        CancellationToken cancellationToken) => (await solution.GetTypesByMetadataNameAsync([fullyQualifiedMetadataName], cancellationToken)).FirstOrDefault();


    private static async Task<IReadOnlyList<INamedTypeSymbol>> GetTypesByMetadataNameAsync(
        this Solution solution,
        IEnumerable<string> fullyQualifiedMetadataNames,
        CancellationToken cancellationToken)
    {
        var resolved = new List<INamedTypeSymbol>();
        var names = fullyQualifiedMetadataNames.ToList();
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
            {
                continue;
            }

            foreach (var name in names)
            {
                var type = compilation?.GetTypeByMetadataName(name);
                if (type != null && resolved.All(t => !SymbolEqualityComparer.Default.Equals(t, type)))
                {
                    resolved.Add(type);
                }
            }
            if (resolved.Count == names.Count)
            {
                break;
            }
        }

        return resolved;
    }

    public static async IAsyncEnumerable<WebApiServiceClientCall> FindServiceClientCallsAsync(
        this Solution solution,
        string serviceClientRootNamespace,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var foundSomething = false;
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
            {
                continue;
            }

            if (foundSomething)
            {
                break;
            }

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                var invocations = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>();

                foreach (var invocation in invocations)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(invocation);

                    if (symbolInfo.Symbol is IMethodSymbol methodSymbol
                        && methodSymbol.ContainingNamespace.ToDisplayString().StartsWith(serviceClientRootNamespace))
                    {
                        var callStacks = await methodSymbol.GenerateCallStacksAsync(solution, DefaultCallerFilter, AnyStackIsValidFilter.Default, cancellationToken).ToListAsync(cancellationToken);
                        foreach (var callStack in callStacks)
                        {
                            var externalCalls = callStack.Calls
                                .Where(e => e.CallingSymbol is IMethodSymbol)
                                .SkipWhile(c => ((IMethodSymbol)c.CallingSymbol).ContainingNamespace.ToDisplayString().StartsWith(serviceClientRootNamespace))
                                .ToList();

                            foreach (var externalCall in externalCalls)
                            {
                                foundSomething = true;
                                var arguments = await externalCall.GetInvocationArgumentsAsync(solution, cancellationToken).ToListAsync(cancellationToken);
                                if (arguments.FirstOrDefault() is INamedTypeSymbol serializedArgumentType && serializedArgumentType.ToDisplayString() != "?")
                                {
                                    yield return new WebApiServiceClientCall(
                                        Method: methodSymbol.ToNamedMethodSymbol(),
                                        CallStack: callStack,
                                        Signature: new MethodSignature(
                                            ReturnValue: methodSymbol.ReturnsVoid ? null : methodSymbol.ReturnType.ToNamedTypeSymbol(),
                                            Parameters: methodSymbol.Parameters.Select(ToNamedTypeSymbol).ToList()));
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public static readonly Func<SymbolCallerInfo, bool> DefaultCallerFilter = c => !c.CalledSymbol.ContainingType.IsController()
        && !c.CallingSymbol.IsErpViewFrameworkClass()
        && !c.CalledSymbol.IsWorkflow()
        && !c.CalledSymbol.IsModificationStrategyManagerCall();

    // True when a reverse-call-graph edge steps from a request handler up into the in-process mediator dispatcher
    // (Marinator in the monolith, MediatR in the module). Marinator/MediatR resolve every command through the single
    // generic IRequestHandler<T>.HandleAsync symbol, so once the walk reaches the dispatcher, FindCallersAsync returns
    // the dispatch sites of ALL commands and the stack fans out to unrelated commands. The crossing edge is recognised
    // by its CalledSymbol being the mediator's IRequestHandler interface member.
    private static bool StepsIntoMediatorDispatch(SymbolCallerInfo caller)
    {
        var calledSymbol = caller.CalledSymbol.ToDisplayString();
        return calledSymbol.StartsWith(MarinatorConstants.RequestHandlerInterfaceFullName)
            || calledSymbol.StartsWith($"{MediatorConstants.Namespace}.{MediatorConstants.RequestHandlerInterfaceName}");
    }

    // DefaultCallerFilter, additionally refusing to cross into the Marinator/MediatR dispatcher. Stopping there
    // terminates the stack cleanly at the handler that produced the message, correctly attributing it to that slice
    // instead of leaking across the mediator boundary. Used by the NServiceBus/Kafka reverse-call-graph finders.
    public static readonly Func<SymbolCallerInfo, bool> CallerFilterStoppingAtMediatorHandlers =
        c => DefaultCallerFilter(c) && !StepsIntoMediatorDispatch(c);

    public class AnyStackIsValidFilter : ICallStackFilter
    {
        public static readonly AnyStackIsValidFilter Default = new ();
        public bool IsValid(IReadOnlyList<SymbolCallerInfo> callStack) => true;
    }

    public interface ICallStackFilter
    {
        public bool IsValid(IReadOnlyList<SymbolCallerInfo> callStack);
    }

    private class CallStackMustBeWithinProviderOrderAfter5LevelsFilter : ICallStackFilter
    {
        public static readonly CallStackMustBeWithinProviderOrderAfter5LevelsFilter Default = new ();
        public bool IsValid(IReadOnlyList<SymbolCallerInfo> callStack)
        {
            if (callStack.Count < 6)
            {
                return true;
            }

            return callStack.Any(e => e.CallingSymbol.ToDisplayString().StartsWith("Dg.ProviderOrder"));
        }
    }

    public static async IAsyncEnumerable<NServiceBusCall> FindNServiceBusCallsAsync(
        this Solution solution,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var relevantTypes = await solution.GetTypesByMetadataNameAsync(
            new[]
            {
                NServiceBusConstants.MessageSessionInterfaceFullName,
                NServiceBusConstants.MessageSessionExtensionsFullName,
                NServiceBusConstants.MessagingScopeInterfaceFullName,
            },
            cancellationToken);
        if (relevantTypes.Count == 0)
        {
            yield break;
        }

        var methods = relevantTypes.SelectMany(
                t => t.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(
                        e =>
                            NServiceBusConstants.EventPublishMethodNames.Contains(e.Name)
                            || NServiceBusConstants.CommandSendMethodNames.Contains(e.Name)))
            .ToList();

        foreach (var method in methods)
        {
            var callStacks = await method.GenerateCallStacksAsync(solution, CallerFilterStoppingAtMediatorHandlers, CallStackMustBeWithinProviderOrderAfter5LevelsFilter.Default, cancellationToken)
                .ToListAsync(cancellationToken);

            foreach (var callStack in callStacks)
            {
                var nonGenericCalls = callStack.Calls.SkipWhile(c => (c.CallingSymbol as IMethodSymbol)?.IsGenericMethod == true).ToList();
                if (nonGenericCalls.Any())
                {
                    var firstNonGenericCall = nonGenericCalls.First();
                    var arguments = await firstNonGenericCall.GetInvocationArgumentsAsync(solution, cancellationToken).ToListAsync(cancellationToken);
                    if (arguments.FirstOrDefault() is INamedTypeSymbol payloadType && payloadType.ToDisplayString() != "?")
                    {
                        yield return new NServiceBusCall(
                            callStack,
                            payloadType.ToNamedTypeSymbol());
                    }
                }
            }
        }
    }

    public static async IAsyncEnumerable<KafkaCall> FindKafkaCallsAsync(
        this Solution solution,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var relevantTypes = await solution.GetTypesByMetadataNameAsync(
            KafkaConstants.ProducerInterfaceFullNames,
            cancellationToken);
        if (relevantTypes.Count == 0)
        {
            yield break;
        }

        var methods = relevantTypes.SelectMany(t => t.GetMembers())
            .OfType<IMethodSymbol>()
            .Where(e => KafkaConstants.ProduceMethodNames.Contains(e.Name))
            .ToList();

        foreach (var method in methods)
        {
            var callStacks = await method.GenerateCallStacksAsync(solution, CallerFilterStoppingAtMediatorHandlers, CallStackMustBeWithinProviderOrderAfter5LevelsFilter.Default, cancellationToken)
                .ToListAsync(cancellationToken);

            foreach (var callStack in callStacks)
            {
                async Task<bool> HasTypeArgumentsAsync(SymbolCallerInfo caller)
                {
                    var arguments = await caller.GetInvocationArgumentsAsync(solution, cancellationToken).ToListAsync(cancellationToken);
                    return arguments.Any(a => a.TypeKind == TypeKind.TypeParameter);
                }

                async Task<SymbolCallerInfo?> GetFirstNonGenericCallAsync(CallStack callStack)
                {
                    foreach (var caller in callStack.Calls)
                    {
                        if ((caller.CallingSymbol as IMethodSymbol)?.IsGenericMethod is true)
                        {
                            continue;
                        }

                        if (await HasTypeArgumentsAsync(caller))
                        {
                            continue;
                        }
                        return caller;
                    }

                    return null;
                }
                var firstNonGenericCall = await GetFirstNonGenericCallAsync(callStack);
                if (firstNonGenericCall is not null)
                {
                    var arguments = await firstNonGenericCall.Value.GetInvocationArgumentsAsync(solution, cancellationToken).ToListAsync(cancellationToken);
                    if (arguments.Count > 2 && arguments[2] is INamedTypeSymbol payloadType && payloadType.ToDisplayString() != "?")
                    {
                        yield return new KafkaCall(
                            callStack,
                            payloadType.ToNamedTypeSymbol());
                    }
                }
            }
        }
    }

    public static async IAsyncEnumerable<KafkaMessageHandler> FindKafkaMessageHandlersAsync(
        this Solution solution,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var interfaceTypes = await solution.GetTypesByMetadataNameAsync(KafkaConstants.MessageHandlerInterfaceFullNamesWithArity,
                cancellationToken);

        foreach (var interfaceType in interfaceTypes)
        {
            var implementations = await SymbolFinder.FindImplementationsAsync(
                type: interfaceType,
                solution: solution,
                cancellationToken: cancellationToken);

            foreach (var implementation in implementations)
            {
                var singleMessageReceiverInterfaces = implementation.Interfaces
                    .Where(i => KafkaConstants.MessageHandlerInterfaceName == i.Name)
                    .ToList();

                var batchMessageReceiverInterfaces = implementation.Interfaces
                    .Where(i => KafkaConstants.BatchMessageHandlerInterfaceName == i.Name)
                    .ToList();

                foreach (var singleMessageReceiverInterface in singleMessageReceiverInterfaces)
                {
                    var payloadType = singleMessageReceiverInterface.TypeArguments[1];
                    var handlerMethod = implementation
                        .GetMembers()
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(
                            e => e.Name == KafkaConstants.MessageHandlerMethodName
                                && payloadType.Equals(
                                    (e.Parameters.FirstOrDefault()?.Type as INamedTypeSymbol)?.TypeArguments[1],
                                    SymbolEqualityComparer.Default));

                    if (handlerMethod != null)
                    {
                        // The payload is the message type TValue (the interface's second type argument), NOT the
                        // handler parameter type: consumers receive it wrapped as ConsumerContext<TKey, TValue>, so
                        // taking the parameter type verbatim would record the wrapper instead of the message and never
                        // match a producer's KafkaCall payload.
                        yield return new KafkaMessageHandler(
                            implementation.ToNamedTypeSymbol(),
                            handlerMethod!.ToNamedMethodSymbol(),
                            false,
                            payloadType.ToNamedTypeSymbol());
                    }
                }

                foreach (var batchMessageReceiverInterface in batchMessageReceiverInterfaces)
                {
                    var payloadType = batchMessageReceiverInterface.TypeArguments[1];
                    var handlerMethod = implementation
                        .GetMembers()
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(
                            e => e.Name == KafkaConstants.BatchMessageHandlerMethodName
                                && payloadType.Equals(
                                    ((e.Parameters.FirstOrDefault()?.Type as INamedTypeSymbol)?.TypeArguments[0] as INamedTypeSymbol)!.TypeArguments[1],
                                    SymbolEqualityComparer.Default));

                    if (handlerMethod != null)
                    {
                        // As above: the payload is the message type TValue (the interface's second type argument).
                        // A batch consumer receives IReadOnlyList<ConsumerContext<TKey, TValue>>, so resolving from the
                        // interface avoids digging TValue back out of that nested parameter type.
                        yield return new KafkaMessageHandler(
                            implementation.ToNamedTypeSymbol(),
                            handlerMethod!.ToNamedMethodSymbol(),
                            true,
                            payloadType.ToNamedTypeSymbol());
                    }
                }
            }
        }
    }

    // Finds every database column a piece of code reads or writes, attributed (later, in FindVerticalSlices) to the
    // vertical slice(s) whose handler reaches it. This is ORM-agnostic: it does not care whether the access goes through
    // EF Core (module) or Deblazer (monolith). An "entity" is simply any type the dbml catalog knows a table for, a
    // "column" is a property that maps to a dbml <Column>, and read-vs-write is decided by the syntactic position of the
    // access. Attribution reuses the same reverse-call-graph machinery as the NServiceBus/Kafka finders.
    //
    // Known best-effort limits (documented for callers): a fully-materialized entity read (a query with no projection)
    // is only captured through the columns its mapping code actually touches, not expanded to every column; an update
    // records only the statically-assigned columns (runtime change-tracking may persist a different set); value-object
    // column renames are handled only for the cases listed in DbmlSchemaCatalog.
    public static async Task<IReadOnlyList<DbColumnAccessCall>> FindDbColumnAccessesAsync(
        this Solution solution,
        DbmlSchemaCatalog catalog,
        CancellationToken cancellationToken)
    {
        // Containing method symbol -> the distinct column accesses lexically inside it.
        var accessesByMethod = new Dictionary<IMethodSymbol, HashSet<DbColumnAccess>>(SymbolEqualityComparer.Default);
        var columnNames = catalog.AllColumnNames;

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
            {
                continue;
            }

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var root = await syntaxTree.GetRootAsync(cancellationToken);
                var semanticModel = compilation.GetSemanticModelOrNull(syntaxTree);
                if (semanticModel is null)
                {
                    continue;
                }

                // Namespaces in scope for this file (its `using` directives plus its own declared namespaces). Used to
                // disambiguate the name-based fallback when an entity type failed to bind (see TryResolveTableByName).
                var contextNamespaces = new HashSet<string>(StringComparer.Ordinal);
                foreach (var u in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
                {
                    if (u.Name is not null)
                    {
                        contextNamespaces.Add(u.Name.ToString());
                    }
                }
                foreach (var ns in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
                {
                    contextNamespaces.Add(ns.Name.ToString());
                }

                // Member accesses (x.Col): a write when it is the target of an assignment/increment, a read otherwise.
                // Pre-filter on the member name against the set of all column names to avoid resolving the semantic
                // model for the millions of member accesses (`.ToList()`, `.Where(...)`, ...) that cannot be columns.
                foreach (var memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
                {
                    var memberName = memberAccess.Name.Identifier.Text;
                    if (!columnNames.Contains(memberName))
                    {
                        continue;
                    }

                    var receiverExpression = memberAccess.Expression;
                    var receiverType = semanticModel.GetTypeInfo(receiverExpression, cancellationToken).Type;

                    var table = receiverType is INamedTypeSymbol namedReceiver ? catalog.TryResolveTable(namedReceiver) : null;
                    if (table is null && (receiverType is null || receiverType.TypeKind == TypeKind.Error))
                    {
                        // The receiver's entity type is not in the compilation (the Deblazer/EF entity classes are
                        // code-generated and absent when the solution is opened without a full build), so it binds to a
                        // Roslyn Error type. Recover the table without a bound symbol, in order of confidence:
                        //   1) the error symbol's own name, when it survived (explicit `dbContext.Set<XDbModel>()`);
                        //   2) failing that, from the receiver SYNTAX — a navigation hop (`e.OrderExport.OrderId`) names
                        //      its target table, and a query lambda parameter (`WhereDb(p => p.Col)`) resolves to the
                        //      entity produced at the root of its query chain (see ResolveErrorTypedReceiverTable).
                        // Every candidate name is validated against the dbml catalog, so a non-entity name simply
                        // resolves to nothing rather than being misattributed.
                        table = receiverType is INamedTypeSymbol errorNamed
                            ? catalog.TryResolveTableByName(errorNamed.Name, contextNamespaces)
                            : null;
                        table ??= ResolveErrorTypedReceiverTable(receiverExpression, catalog, contextNamespaces);
                    }
                    if (table is null)
                    {
                        continue;
                    }

                    var column = catalog.ResolveColumn(table, memberName);
                    if (column is null)
                    {
                        catalog.RecordUnresolvedColumn(table, memberName);
                        continue;
                    }

                    var kind = IsWritePosition(memberAccess) ? DbAccessKind.Write : DbAccessKind.Read;
                    AddAccess(accessesByMethod, semanticModel, memberAccess, new DbColumnAccess(table.Database, table.Schema, table.Table, column, kind));
                }

                // Object initializers (new Entity { Col = ... }): each initialized column is a write.
                foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                {
                    if (creation.Initializer is null)
                    {
                        continue;
                    }

                    var createdTypeName = creation.Type switch
                    {
                        IdentifierNameSyntax id => id.Identifier.Text,
                        QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
                        GenericNameSyntax generic => generic.Identifier.Text,
                        _ => null,
                    };
                    if (createdTypeName is null || !catalog.IsCandidateEntityTypeName(createdTypeName))
                    {
                        continue;
                    }

                    var createdType = semanticModel.GetTypeInfo(creation, cancellationToken).Type as INamedTypeSymbol;
                    var table = createdType is null ? null : catalog.TryResolveTable(createdType);
                    if (table is null && (createdType is null || createdType.TypeKind == TypeKind.Error))
                    {
                        // Entity type absent from the compilation (generated code) — resolve by the syntactic type name,
                        // which is always available here (unlike inferred receivers). Recovers `new Entity { Col = ... }`
                        // writes even when the entity class itself does not bind.
                        table = catalog.TryResolveTableByName(createdTypeName, contextNamespaces);
                    }
                    if (table is null)
                    {
                        continue;
                    }

                    foreach (var assignment in creation.Initializer.Expressions.OfType<AssignmentExpressionSyntax>())
                    {
                        if (assignment.Left is not IdentifierNameSyntax propertyName)
                        {
                            continue;
                        }

                        var column = catalog.ResolveColumn(table, propertyName.Identifier.Text);
                        if (column is null)
                        {
                            catalog.RecordUnresolvedColumn(table, propertyName.Identifier.Text);
                            continue;
                        }

                        AddAccess(accessesByMethod, semanticModel, creation, new DbColumnAccess(table.Database, table.Schema, table.Table, column, DbAccessKind.Write));
                    }
                }
            }
        }

        // Attribute each method's accesses via its reverse call graph, generated once per method (a method typically
        // hosts many column accesses). Stopping at the mediator dispatcher keeps a write inside a handler attributed to
        // that handler's slice, exactly like the NServiceBus/Kafka finders.
        var results = new List<DbColumnAccessCall>();
        foreach (var (method, accesses) in accessesByMethod)
        {
            var callStacks = await method
                .GenerateCallStacksAsync(solution, CallerFilterStoppingAtMediatorHandlers, CallStackMustBeWithinProviderOrderAfter5LevelsFilter.Default, cancellationToken)
                .ToListAsync(cancellationToken);

            results.Add(new DbColumnAccessCall(method.ToNamedMethodSymbol(), callStacks, accesses.ToList()));
        }

        return results;
    }

    private static bool IsWritePosition(ExpressionSyntax expression) =>
        expression.Parent switch
        {
            AssignmentExpressionSyntax assignment => assignment.Left == expression,
            PostfixUnaryExpressionSyntax => true,
            PrefixUnaryExpressionSyntax prefix => prefix.OperatorToken.Text is "++" or "--",
            _ => false,
        };

    private static void AddAccess(
        Dictionary<IMethodSymbol, HashSet<DbColumnAccess>> accessesByMethod,
        SemanticModel semanticModel,
        SyntaxNode node,
        DbColumnAccess access)
    {
        var method = ResolveContainingMethod(semanticModel, node);
        if (method is null)
        {
            return;
        }

        if (!accessesByMethod.TryGetValue(method, out var set))
        {
            set = new HashSet<DbColumnAccess>();
            accessesByMethod[method] = set;
        }

        set.Add(access);
    }

    // The nearest enclosing ordinary member (method/accessor/constructor), deliberately climbing out of lambdas and
    // local functions so an access inside a `.Where(x => x.Col == ...)` lambda is attributed to the repository method
    // that hosts the query — which is what the reverse call graph connects to a slice's handler.
    private static IMethodSymbol? ResolveContainingMethod(SemanticModel semanticModel, SyntaxNode node)
    {
        var declaration = node.FirstAncestorOrSelf<SyntaxNode>(n =>
            n is MethodDeclarationSyntax or AccessorDeclarationSyntax or ConstructorDeclarationSyntax);

        return declaration is null ? null : semanticModel.GetDeclaredSymbol(declaration) as IMethodSymbol;
    }

    // Recovers the table for an error-typed member-access receiver purely from syntax, used when the entity type did
    // not bind (generated ORM code absent from the design-time compilation) and its error symbol carried no usable
    // name. Two shapes are handled, both matched by name against the dbml catalog (so a stray non-entity name simply
    // resolves to nothing rather than being misattributed):
    //   * a navigation hop `<expr>.Nav.Column` — `Nav` is the target entity/table name (e.g. `e.OrderExport.OrderId`);
    //   * a query lambda parameter `Producer()...WhereDb(p => p.Column)` — `p` is the element type of the query, which
    //     is the entity produced at the chain root (Deblazer `dbWrite.ProviderOrders()` / EF `dbContext.Set<T>()`),
    //     advanced by any `Join<Entity>()` operator that precedes the lambda.
    private static DbmlSchemaCatalog.TableInfo? ResolveErrorTypedReceiverTable(
        ExpressionSyntax receiver,
        DbmlSchemaCatalog catalog,
        IReadOnlyCollection<string> contextNamespaces)
    {
        switch (receiver)
        {
            case MemberAccessExpressionSyntax navigation:
                return catalog.TryResolveTableByName(navigation.Name.Identifier.Text, contextNamespaces);
            case IdentifierNameSyntax parameterReference:
                var entityName = TryGetQueryElementEntityName(parameterReference);
                return entityName is null ? null : catalog.TryResolveTableByName(entityName, contextNamespaces);
            default:
                return null;
        }
    }

    // For an identifier that is a query lambda's parameter (`p` in `.WhereDb(p => ...)`), returns the name of the entity
    // that parameter iterates: the entity produced at the root of the query chain, advanced by any `Join<Entity>()`
    // between the root and this lambda. Returns null when the identifier is not such a parameter or the root producer
    // cannot be identified. Purely syntactic; the caller validates the returned name against the dbml catalog.
    private static string? TryGetQueryElementEntityName(IdentifierNameSyntax parameterReference)
    {
        var parameterName = parameterReference.Identifier.Text;

        // Nearest enclosing lambda that declares this parameter name (handles nested query lambdas — the inner one wins).
        LambdaExpressionSyntax? lambda = null;
        foreach (var ancestor in parameterReference.Ancestors())
        {
            if (ancestor is SimpleLambdaExpressionSyntax simple && simple.Parameter.Identifier.Text == parameterName)
            {
                lambda = simple;
                break;
            }

            if (ancestor is ParenthesizedLambdaExpressionSyntax paren
                && paren.ParameterList.Parameters.Any(p => p.Identifier.Text == parameterName))
            {
                lambda = paren;
                break;
            }
        }

        if (lambda?.Parent is not ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax lambdaInvocation } })
        {
            return null;
        }

        // Collect the invocation chain from this lambda's operator down to the producer at the root of the chain.
        var chain = new List<InvocationExpressionSyntax>();
        var current = lambdaInvocation;
        while (true)
        {
            chain.Add(current);
            if (current.Expression is MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax inner })
            {
                current = inner;
                continue;
            }

            break;
        }

        chain.Reverse(); // producer first, this lambda's operator last

        var entityName = GetProducerEntityName(chain[0]);
        if (entityName is null)
        {
            return null;
        }

        // Apply the joins that occur before this lambda's operator: each `Join<Entity>()` re-roots the element type, so
        // the lambda parameter iterates the most recently joined entity rather than the original producer entity.
        for (var i = 1; i < chain.Count - 1; i++)
        {
            var methodName = GetInvokedMethodName(chain[i]);
            if (methodName is not null && methodName.StartsWith("Join", StringComparison.Ordinal) && methodName.Length > "Join".Length)
            {
                entityName = methodName["Join".Length..];
            }
        }

        return entityName;
    }

    // The entity a query-root producer yields: the type argument of an EF `Set<T>()`/`Query<T>()`, otherwise the
    // producer method's own name (Deblazer generates one query method per table, named after the table's dbml `Member`,
    // e.g. `ProviderOrders()` -> the `ProviderOrder` table).
    private static string? GetProducerEntityName(InvocationExpressionSyntax producer)
    {
        var nameSyntax = (producer.Expression as MemberAccessExpressionSyntax)?.Name
            ?? producer.Expression as SimpleNameSyntax;

        if (nameSyntax is GenericNameSyntax generic)
        {
            if (generic.Identifier.Text is "Set" or "Query" && generic.TypeArgumentList.Arguments.Count == 1)
            {
                return SimpleTypeName(generic.TypeArgumentList.Arguments[0]);
            }

            // A generic Deblazer producer such as `ProviderOrderItemProducts<int>()`: the entity is the method name, the
            // type argument is the key type.
            return generic.Identifier.Text;
        }

        return nameSyntax?.Identifier.Text;
    }

    private static string? GetInvokedMethodName(InvocationExpressionSyntax invocation) =>
        ((invocation.Expression as MemberAccessExpressionSyntax)?.Name
            ?? invocation.Expression as SimpleNameSyntax)?.Identifier.Text;

    private static string? SimpleTypeName(TypeSyntax type) => type switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        GenericNameSyntax generic => generic.Identifier.Text,
        QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
        _ => null,
    };

    private static async IAsyncEnumerable<CallStack> GenerateCallStacksRecursiveAsync(
        this IMethodSymbol method,
        Solution solution,
        List<SymbolCallerInfo> currentStack,
        Func<SymbolCallerInfo, bool> callerFilter,
        ICallStackFilter stackFilter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Find all callers of the current method
        var callers = (await SymbolFinder.FindCallersAsync(method, solution, cancellationToken))
            .Where(c => !c.CallingSymbol.Equals(c.CalledSymbol, SymbolEqualityComparer.Default))
            .Where(c => !currentStack.Any(c2 => SymbolEqualityComparer.Default.Equals(c.CallingSymbol, c2.CallingSymbol)))
            .Where(callerFilter)
            .ToList();

        // Check if the method has any callers
        if (callers.Any())
        {
            // If the method has callers, recursively search each caller
            foreach (var caller in callers)
            {
                if (caller.CallingSymbol is IMethodSymbol callerMethod)
                {
                    var stackCopy = currentStack.ToList();
                    stackCopy.Add(caller);
                    if (stackFilter.IsValid(stackCopy))
                    {
                        await foreach (var result in callerMethod.GenerateCallStacksRecursiveAsync(
                                           solution,
                                           stackCopy,
                                           callerFilter,
                                           stackFilter,
                                           cancellationToken))
                        {
                            yield return result;
                        }
                    }
                }
            }
        }
        else if (currentStack.Any())
        {
            yield return new CallStack(currentStack);
        }
    }
}