// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Dg.ProviderOrder.Mokumentation;
using Dg.ProviderOrder.Mokumentation.ArchitectureBricks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualBasic;
using RestSharp;

internal partial class Program
{
    // Builds primary adapters from MediatR/Marinator request handlers by matching, on request/command type, each
    // handler (a primary port) against the dispatch calls that send that request. The method that directly issues the
    // dispatch becomes the adapter: an NServiceBus message handler makes it an NServiceBus adapter (carrying the
    // incoming payload), otherwise it is treated as a Web adapter. Used for both the module (MediatR) and the monolith
    // (Marinator) command slices.
    private static IEnumerable<PrimaryAdapter> FindPrimaryAdaptersFromRequestHandlers(
        IReadOnlyList<MediatrRequestHandler> mediatrRequestHandlers,
        IReadOnlyList<MediatorCall> mediatorCalls,
        IReadOnlyList<NServiceBusMessageHandler> nServiceBusMessageHandlers)
    {
        var tuples = mediatrRequestHandlers
            .Join(
                mediatorCalls,
                p => p.RequestType.ToDisplayString(),
                call => call.RequestType.ToDisplayString(),
                (mediatrRequestHandler, mediatorCall) => (MediatrRequestHandler: mediatrRequestHandler, MediatorCall: mediatorCall))
            .ToList();

        foreach (var tuple in tuples)
        {
            var topLevelClassType = tuple.MediatorCall.CallStack.Calls[0].CallingSymbol.ContainingType;
            var entryMethod = (tuple.MediatorCall.CallStack.Calls[0].CallingSymbol as IMethodSymbol)!;
            var correspondingMServiceBusMessageHandler = nServiceBusMessageHandlers.FirstOrDefault(h => h.ImplementationMethod.Equals(entryMethod.ToNamedMethodSymbol()));
            if (correspondingMServiceBusMessageHandler != null)
            {
                yield return new PrimaryAdapter(
                    topLevelClassType.ToNamedTypeSymbol(),
                    entryMethod.ToNamedMethodSymbol(),
                    tuple.MediatrRequestHandler,
                    PrimaryAdapterType.NServiceBus,
                    correspondingMServiceBusMessageHandler.PayloadType);
            }
            else
            {
                yield return new PrimaryAdapter(
                    ClassTypeSymbol: topLevelClassType.ToNamedTypeSymbol(),
                    EntryMethod: entryMethod.ToNamedMethodSymbol(),
                    tuple.MediatrRequestHandler,
                    PrimaryAdapterType.Web,
                    null);
            }
        }
    }


}