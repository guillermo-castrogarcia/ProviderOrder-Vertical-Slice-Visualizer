using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using InvocationExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax;

namespace Dg.ProviderOrder.Mokumentation;

using ArchitectureBricks;
using LibGit2Sharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class RoslynDgExtensions
{
    public static bool IsController(this INamedTypeSymbol symbol)
    {
        return symbol.BaseType != null && SourceCodeConstants.AllControllerBaseClassFullNames.Any(t => t == symbol.BaseType.ToDisplayString());
    }

    public static NamedTypeSymbol ToNamedTypeSymbol(this IParameterSymbol symbol) =>
        new(
            symbol.Name,
            FullName.FromValue(symbol.ToDisplayString()));

    public static NamedTypeSymbol ToNamedTypeSymbol(this ITypeSymbol symbol) =>
        new(
            symbol.Name,
            FullName.FromValue(symbol.ToDisplayString()));

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
            Parameters: symbol.Parameters.Select(ToNamedTypeSymbol).ToList()));

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
        Repository repository,
        CancellationToken cancellationToken) =>
        solution.FindRequestHandlersAsync(
            MediatorConstants.RequestHandlerInterfaceFullNamesithArity,
            MediatorConstants.RequestHandlerInterfaceName,
            MediatorConstants.RequestHandlerMethodName,
            PrimaryPortType.MediatrRequestHandler,
            repository,
            cancellationToken);

    // Marinator (monolith) command handlers. Marinator is MediatR-shaped, so this reuses the exact same detection.
    public static IAsyncEnumerable<MediatrRequestHandler> FindMarinatorRequestHandlersAsync(
        this Solution solution,
        Repository repository,
        CancellationToken cancellationToken) =>
        solution.FindRequestHandlersAsync(
            MarinatorConstants.RequestHandlerInterfaceFullNamesWithArity,
            MarinatorConstants.RequestHandlerInterfaceName,
            MarinatorConstants.RequestHandlerMethodName,
            PrimaryPortType.MarinatorRequestHandler,
            repository,
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
        Repository repository,
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
                    var document = await handlerMethod.FindDocumentAsync(solution, cancellationToken);
                    var versionControlInfo = document is null
                        ? null
                        : AzureDevOpsVersionControlInfo.Create(document, repository);
                    yield return new MediatrRequestHandler(
                        implementation.ToNamedTypeSymbol(),
                        handlerMethod.ToNamedMethodSymbol(),
                        handlerMethod.Parameters[0].Type.ToNamedTypeSymbol(),
                        PrimaryPortType: primaryPortType,
                        versionControlInfo);
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