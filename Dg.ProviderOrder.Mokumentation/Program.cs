// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Dg.ProviderOrder.Mokumentation;
using Dg.ProviderOrder.Mokumentation.ArchitectureBricks;
using Dg.ProviderOrder.Mokumentation.Schema;
using LibGit2Sharp;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualBasic;
using RestSharp;

internal partial class Program
{

    const string moduleRepositoryPath = @"C:\Development\Dg.ProviderOrder-4\";
    const string moduleSolutionPath = @$"{moduleRepositoryPath}Dg.ProviderOrder.slnx";
    const string monolithRepositoryPath =
        @"C:\Development\devinite-2\";
    const string monolithSolutionPath =
        @$"{monolithRepositoryPath}src\ProviderOrder\Dg.ProviderOrderComponentHost.slnx";

    // Resolves to this project's source directory (where the generated *.verticalslices.json files live), independent
    // of the machine or the working directory the app is launched from.
    private static string OutputDirectory([CallerFilePath] string thisFilePath = "") => Path.GetDirectoryName(thisFilePath)!;

    static async Task Main(string[] args)
    {
        // Required so MSBuildWorkspace uses the installed .NET SDK's MSBuild (which can evaluate the SDK-style projects
        // and parse the .slnx solution format). Must run before the first use of any MSBuild type.
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        await RetrieveMonolithVerticalSlices(CancellationToken.None);
        await RetrieveModuleVerticalSlices(CancellationToken.None);
    }

    private static async Task RetrieveMonolithVerticalSlices(CancellationToken cancellationToken)
    {
        using var repository = new Repository(monolithRepositoryPath);
        var sln = await OpenSolutionAndRemoveTestProjects(monolithSolutionPath, cancellationToken);

        var providerOrderInterfaceServiceClientCalls =
            await sln.FindServiceClientCallsAsync(ServiceClientConstants.ProviderOrderInterfaceServiceClientRootNamespace, cancellationToken).ToListAsync(cancellationToken);

        var providerOrderServiceClientCalls = await sln.FindServiceClientCallsAsync(ServiceClientConstants.ProviderOrderServiceClientRootNamespace, cancellationToken).ToListAsync(cancellationToken);

        var nserviceBusCalls = await sln.FindNServiceBusCallsAsync(cancellationToken).ToListAsync(cancellationToken);

        var nserviceBusHandlers = await sln.FindNServiceBusMessageHandlersAsync(cancellationToken).ToListAsync(cancellationToken);
        var kafkaHandlers = await sln.FindKafkaMessageHandlersAsync(cancellationToken).ToListAsync(cancellationToken);

        var kafkaCalls = await sln.FindKafkaCallsAsync(cancellationToken).ToListAsync(cancellationToken);

        var controllerActions = (await sln.FindControllerActionsAsync(cancellationToken).ToListAsync(cancellationToken)).ToHashSet().ToList();

        // Command vertical slices in the monolith are dispatched through the Marinator framework. Because Marinator
        // resolves handlers by reflection, call-stack analysis cannot reach them from the application-service side, so
        // those slices are detected the same way the module detects MediatR slices: match the command type at the
        // IMarinator.SendAsync call site against the Marinator IRequestHandler that handles it.
        var marinatorRequestHandlers = await sln.FindMarinatorRequestHandlersAsync(cancellationToken).ToListAsync(cancellationToken);
        var marinatorCalls = await sln.FindMarinatorCallsAsync(cancellationToken).ToListAsync(cancellationToken);

        var marinatorRequestHandlerAndCallTuples = marinatorRequestHandlers
            .Join(
                marinatorCalls,
                p => p.RequestType.ToDisplayString(),
                call => call.RequestType.ToDisplayString(),
                (mediatrRequestHandler, mediatorCall) => (MediatrRequestHandler: mediatrRequestHandler, MediatorCall: mediatorCall))
            .Select(t => new MarinatorOrMediatorRequestHandlerAndCall(
                MediatrRequestHandler: t.MediatrRequestHandler,
                MarinatorCall: t.MediatorCall))
            .ToList();


        var primaryAdapters = FindPrimaryAdapters(marinatorRequestHandlerAndCallTuples, controllerActions, nserviceBusHandlers, kafkaHandlers).ToList();

        var dbSchemaCatalog = DbmlSchemaCatalog.LoadFromRepositories(monolithRepositoryPath);
        Console.WriteLine($"[monolith] dbml catalog: {dbSchemaCatalog.DatabaseCount} databases, {dbSchemaCatalog.TableCount} tables");
        var dbColumnAccesses = await sln.FindDbColumnAccessesAsync(dbSchemaCatalog, cancellationToken);
        dbSchemaCatalog.ReportUnresolved("monolith");

        var verticalSlices = FindVerticalSlices(
                primaryAdapters,
                nserviceBusCalls,
                kafkaCalls,
                webApiServiceClientCalls: providerOrderInterfaceServiceClientCalls.Union(providerOrderServiceClientCalls).ToList(),
                dbColumnAccesses: dbColumnAccesses)
            .ToList();

        var extract = BuildExtract(verticalSlices, repository);
        var serialized = System.Text.Json.JsonSerializer.Serialize(extract);
        await File.WriteAllTextAsync(
            path: Path.Combine(OutputDirectory(), "monolith.verticalslices.json"),
            contents: serialized);
    }

    private static async Task RetrieveModuleVerticalSlices(CancellationToken cancellationToken)
    {
        using var repository = new Repository(moduleRepositoryPath);
        var sln = await OpenSolutionAndRemoveTestProjects(moduleSolutionPath, cancellationToken);
        var providerOrderInterfaceServiceClientCalls =
            await sln.FindServiceClientCallsAsync(ServiceClientConstants.ProviderOrderInterfaceServiceClientRootNamespace, cancellationToken).ToListAsync(cancellationToken);

        var providerOrderServiceClientCalls = await sln.FindServiceClientCallsAsync(ServiceClientConstants.ProviderOrderServiceClientRootNamespace, cancellationToken).ToListAsync(cancellationToken);

        var nserviceBusCalls = await sln.FindNServiceBusCallsAsync(cancellationToken).ToListAsync(cancellationToken);

        var nserviceBusHandlers = await sln.FindNServiceBusMessageHandlersAsync(cancellationToken).ToListAsync(cancellationToken);
        var kafkaHandlers = await sln.FindKafkaMessageHandlersAsync(cancellationToken).ToListAsync(cancellationToken);

        var kafkaCalls = await sln.FindKafkaCallsAsync(cancellationToken).ToListAsync(cancellationToken);

        var controllerActions = (await sln.FindControllerActionsAsync(cancellationToken).ToListAsync(cancellationToken)).ToHashSet().ToList();
        var mediatrRequestHandlers = await sln.FindMediatrRequestHandlersAsync(cancellationToken).ToListAsync(cancellationToken);
        var mediatorCalls = await sln.FindMediatorCallsAsync(cancellationToken).ToListAsync(cancellationToken);
        var mediatorRequestHandlerAndCallTuples = mediatrRequestHandlers
            .Join(
                mediatorCalls,
                p => p.RequestType.ToDisplayString(),
                call => call.RequestType.ToDisplayString(),
                (mediatrRequestHandler, mediatorCall) => (MediatrRequestHandler: mediatrRequestHandler, MediatorCall: mediatorCall))
            .Select(t => new MarinatorOrMediatorRequestHandlerAndCall(
                MediatrRequestHandler: t.MediatrRequestHandler,
                MarinatorCall: t.MediatorCall))
            .ToList();

        var primaryAdapters = FindPrimaryAdapters(mediatorRequestHandlerAndCallTuples, controllerActions, nserviceBusHandlers, kafkaHandlers).ToList();

        var dbSchemaCatalog = DbmlSchemaCatalog.LoadFromRepositories(moduleRepositoryPath);
        Console.WriteLine($"[module] dbml catalog: {dbSchemaCatalog.DatabaseCount} databases, {dbSchemaCatalog.TableCount} tables");
        var dbColumnAccesses = await sln.FindDbColumnAccessesAsync(dbSchemaCatalog, cancellationToken);
        dbSchemaCatalog.ReportUnresolved("module");

        var verticalSlices = FindVerticalSlices(
                primaryAdapters,
                nserviceBusCalls,
                kafkaCalls,
                webApiServiceClientCalls: providerOrderInterfaceServiceClientCalls.Union(providerOrderServiceClientCalls).ToList(),
                dbColumnAccesses: dbColumnAccesses)
            .ToList();

        var extract = BuildExtract(verticalSlices, repository);
        var serialized = System.Text.Json.JsonSerializer.Serialize(extract);
        await File.WriteAllTextAsync(
            path: Path.Combine(OutputDirectory(), "module.verticalslices.json"),
            contents: serialized);
    }

    private static async Task<Solution> OpenSolutionAndRemoveTestProjects(
        string filePath,
        CancellationToken cancellationToken)
    {
        var workspace = MSBuildWorkspace.Create();
        var sln = await workspace.OpenSolutionAsync(filePath, cancellationToken: cancellationToken);
        var validProjectIds = sln.Projects.Where(p => !p.Name.Contains("Test"))
            .Select(e => e.Id)
            .ToHashSet();

        var allProjectIds = sln.Projects.Select(e => e.Id).ToHashSet();
        var projectsToRemove = allProjectIds.Except(validProjectIds).ToHashSet();
        foreach (var projectToRemove in projectsToRemove)
        {
            sln = sln.RemoveProject(projectToRemove);
        }

        return sln;
    }

    private static IEnumerable<PrimaryAdapter> FindPrimaryAdapters(
        IReadOnlyList<MarinatorOrMediatorRequestHandlerAndCall> marinatorRequestHandlerAndCalls,
        IReadOnlyList<ControllerAction> controllerActions,
        IReadOnlyList<NServiceBusMessageHandler> nServiceBusMessageHandlers,
        IReadOnlyList<KafkaMessageHandler> kafkaMessageHandlers)
    {
        var callsFromController = marinatorRequestHandlerAndCalls
            .Where(e => e.MarinatorCall.CallStackTopMethod != null)
            .Join(
                controllerActions,
                marinatorRequestHandlerAndCall => marinatorRequestHandlerAndCall.MarinatorCall.CallStackTopMethod!,
                a => a.EntryMethod,
                (marinatorRequestHandlerAndCall, primaryAdapter) => (marinatorRequestHandlerAndCall: marinatorRequestHandlerAndCall, primaryAdapter))
            .ToList();

        foreach (var callFromController in callsFromController)
        {
            yield return new PrimaryAdapter(
                ClassTypeSymbol: callFromController.primaryAdapter.ClassTypeSymbol,
                EntryMethod: callFromController.primaryAdapter.EntryMethod,
                CalledPort: callFromController.marinatorRequestHandlerAndCall.MediatrRequestHandler,
                AdapterType: PrimaryAdapterType.Web,
                PayloadType: null);
        }

        List<(MarinatorOrMediatorRequestHandlerAndCall MarinatorRequestHandlerAndCall, NServiceBusMessageHandler primaryAdapter)>? callsFromMessageHandler = marinatorRequestHandlerAndCalls
            .Where(e => e.MarinatorCall.CallStackTopMethod != null)
            .Join(
                nServiceBusMessageHandlers,
                s => s.MarinatorCall.CallStackTopMethod!,
                a => a.ImplementationMethod,
                (marinatorRequestHandlerAndCall, primaryAdapter) => (marinatorRequestHandlerAndCall: marinatorRequestHandlerAndCall, primaryAdapter))
            .ToList();

        foreach (var callFromMessageHandler in callsFromMessageHandler)
        {
            yield return new PrimaryAdapter(
                ClassTypeSymbol: callFromMessageHandler.primaryAdapter.MessageHandlerImplementation,
                EntryMethod: callFromMessageHandler.primaryAdapter.ImplementationMethod,
                CalledPort: callFromMessageHandler.MarinatorRequestHandlerAndCall.MediatrRequestHandler,
                AdapterType: PrimaryAdapterType.NServiceBus,
                PayloadType: callFromMessageHandler.primaryAdapter.PayloadType);
        }

        List<(MarinatorOrMediatorRequestHandlerAndCall marinatorRequestHandlerAndCall, KafkaMessageHandler primaryAdapter)>? callsFromKafkaMessageHandler = marinatorRequestHandlerAndCalls
            .Where(e => e.MarinatorCall.CallStackTopMethod != null)
            .Join(
                kafkaMessageHandlers,
                s => s.MarinatorCall.CallStackTopMethod!,
                a => a.ImplementationMethod,
                (marinatorRequestHandlerAndCall, primaryAdapter) => (marinatorRequestHandlerAndCall: marinatorRequestHandlerAndCall, primaryAdapter))
            .ToList();

        foreach (var callFromKafkaMessageHandler in callsFromKafkaMessageHandler)
        {
            yield return new PrimaryAdapter(
                ClassTypeSymbol: callFromKafkaMessageHandler.primaryAdapter.MessageHandlerImplementation,
                EntryMethod: callFromKafkaMessageHandler.primaryAdapter.ImplementationMethod,
                CalledPort: callFromKafkaMessageHandler.marinatorRequestHandlerAndCall.MediatrRequestHandler,
                AdapterType: PrimaryAdapterType.Kafka,
                PayloadType: callFromKafkaMessageHandler.primaryAdapter.PayloadType);
        }
    }

    private static IEnumerable<VerticalSlice> FindVerticalSlices(
        IReadOnlyList<PrimaryAdapter> primaryAdapters,
        IReadOnlyList<NServiceBusCall> nServiceBusCalls,
        IReadOnlyList<KafkaCall> kafkaCalls,
        IReadOnlyList<WebApiServiceClientCall> webApiServiceClientCalls,
        IReadOnlyList<DbColumnAccessCall> dbColumnAccesses)
    {
        var primaryAdaptersBySamePrimaryPort = primaryAdapters.ToLookup(e => e.CalledPort);
        foreach (var primaryAdaptersOfSamePrimaryPort in primaryAdaptersBySamePrimaryPort)
        {
            var primaryPort = primaryAdaptersOfSamePrimaryPort.Key;
            var relevantNServiceBusCalls = nServiceBusCalls
                .Where(c => c.CallStack.ContainsMethodAsCallingSymbol(primaryPort.ImplementationMethod))
                .DistinctBy(e => e.PayloadType)
                .ToList();

            var relevantKafkaCalls = kafkaCalls
                .Where(c => c.CallStack.ContainsMethodAsCallingSymbol(primaryPort.ImplementationMethod))
                .DistinctBy(e => e.PayloadType)
                .ToList();

            var relevantWebApiServiceClientCalls = webApiServiceClientCalls
                .Where(c => c.CallStack.ContainsMethodAsCallingSymbol(primaryPort.ImplementationMethod))
                .Select(e => e.ToSerializableWebApiServiceClientCall())
                .DistinctBy(e => e.Signature)
                .ToList();

            // A DB access belongs to this slice when the handler either performs it directly (the access sits in the
            // handler method itself) or transitively reaches the method that performs it (the handler appears as a
            // calling symbol somewhere in that method's reverse call stacks).
            var relevantDbColumnAccesses = dbColumnAccesses
                .Where(c => c.ContainingMethod.Equals(primaryPort.ImplementationMethod)
                    || c.CallStacks.Any(cs => cs.ContainsMethodAsCallingSymbol(primaryPort.ImplementationMethod)))
                .SelectMany(e => e.Accesses)
                .Distinct()
                .ToList();

            yield return new VerticalSlice(
                PrimaryAdapters: primaryAdaptersOfSamePrimaryPort.ToList(),
                PrimaryPort: primaryPort,
                NServiceBusPayloads: relevantNServiceBusCalls.Select(e => e.PayloadType).ToList(),
                KafkaPayloads: relevantKafkaCalls.Select(e => e.PayloadType).ToList(),
                WebApiServiceClientCalls: relevantWebApiServiceClientCalls,
                DbColumnAccesses: relevantDbColumnAccesses);
        }
    }

    // Relativizes every captured source location against the repo root and wraps the slices with the
    // once-per-file repository/commit header, so links are commit-pinned and no absolute local path leaks.
    private static VerticalSliceExtract BuildExtract(IReadOnlyList<VerticalSlice> slices, Repository repository)
    {
        RelativizeSourceLocations(slices, repository.Info.WorkingDirectory);
        var sourceRepository = GitHubSourceRepository.Create(repository)
            ?? throw new InvalidOperationException(
                $"Could not resolve an 'origin' remote for repository at {repository.Info.WorkingDirectory}.");
        return new VerticalSliceExtract(sourceRepository, slices);
    }

    // Rewrites every SourceLocation.Path reachable from a slice from its absolute capture-time path to a
    // repo-relative, forward-slashed path. Idempotent: the IsPathRooted guard skips already-relativized
    // locations, which matters because the same wrapper instance can be shared across slices.
    private static void RelativizeSourceLocations(IReadOnlyList<VerticalSlice> slices, string repoRoot)
    {
        foreach (var slice in slices)
        {
            Relativize(slice.PrimaryPort.ImplementationClass, repoRoot);
            Relativize(slice.PrimaryPort.ImplementationMethod, repoRoot);
            if (slice.PrimaryPort is MediatrRequestHandler mediatrHandler)
            {
                Relativize(mediatrHandler.RequestType, repoRoot);
            }

            foreach (var adapter in slice.PrimaryAdapters)
            {
                Relativize(adapter.ClassTypeSymbol, repoRoot);
                Relativize(adapter.EntryMethod, repoRoot);
                Relativize(adapter.PayloadType, repoRoot);
            }

            foreach (var payload in slice.NServiceBusPayloads)
            {
                Relativize(payload, repoRoot);
            }

            foreach (var payload in slice.KafkaPayloads)
            {
                Relativize(payload, repoRoot);
            }

            foreach (var call in slice.WebApiServiceClientCalls)
            {
                Relativize(call.Method, repoRoot);
                Relativize(call.Signature, repoRoot);
            }
        }
    }

    private static void Relativize(SourceLocation? location, string repoRoot)
    {
        if (location is not null && Path.IsPathRooted(location.Path))
        {
            location.Path = Path.GetRelativePath(repoRoot, location.Path).Replace('\\', '/');
        }
    }

    private static void Relativize(NamedTypeSymbol? type, string repoRoot) =>
        Relativize(type?.SourceLocation, repoRoot);

    private static void Relativize(NamedMethodSymbol? method, string repoRoot)
    {
        if (method is null)
        {
            return;
        }

        Relativize(method.SourceLocation, repoRoot);
        Relativize(method.Signature, repoRoot);
    }

    private static void Relativize(MethodSignature? signature, string repoRoot)
    {
        if (signature is null)
        {
            return;
        }

        Relativize(signature.ReturnValue, repoRoot);
        foreach (var parameter in signature.Parameters)
        {
            Relativize(parameter, repoRoot);
        }
    }
}