namespace Dg.ProviderOrder.Mokumentation;

using Microsoft.CodeAnalysis.FindSymbols;

public static class MediatorConstants
{
    public const string RequestHandlerInterfaceName = "IRequestHandler";
    public const string Namespace = "MediatR";
    public const string RequestHandlerMethodName = "Handle";

    public static string RequestHandlerInterfaceFullName => $"{Namespace}.{RequestHandlerInterfaceName}";

    public static string[] RequestHandlerInterfaceFullNamesithArity => new[]
    {
        $"{Namespace}.{RequestHandlerInterfaceName}`1",
        $"{Namespace}.{RequestHandlerInterfaceName}`2",
    };

    public static string SenderInterfaceFullName => $"{Namespace}.ISender";
}

// The monolith (devinite-2) routes its command vertical slices through the "Marinator" framework — an in-house,
// MediatR-shaped in-process dispatcher local to the Dg.ProviderOrder project. Structurally it mirrors MediatR:
// command handlers implement IRequestHandler<TRequest> / IRequestHandler<TRequest, TResponse> (short interface name
// "IRequestHandler", handler method "HandleAsync"), and callers dispatch a command via IMarinator.SendAsync(command,
// ...). The command is always the first argument. Because Marinator resolves the handler by reflection at runtime,
// there is no compile-time call edge from the dispatch site to the handler — so, exactly like MediatR, detection
// works by matching the command type at the SendAsync call site against the handler's TRequest.
public static class MarinatorConstants
{
    public const string RequestHandlerInterfaceName = "IRequestHandler";
    public const string Namespace = "Dg.ProviderOrder.Application.Framework.MarinatR";
    public const string RequestHandlerMethodName = "HandleAsync";

    public static string RequestHandlerInterfaceFullName => $"{Namespace}.{RequestHandlerInterfaceName}";

    public static string[] RequestHandlerInterfaceFullNamesWithArity => new[]
    {
        $"{Namespace}.{RequestHandlerInterfaceName}`1",
        $"{Namespace}.{RequestHandlerInterfaceName}`2",
    };

    public static string DispatcherInterfaceFullName => $"{Namespace}.IMarinator";
}

public static class NServiceBusConstants
{

    public static readonly string[] CommandSendMethodNames = new[]
    {
        "Send"
    };

    public static readonly string[] EventPublishMethodNames = new[]
    {
        "Publish"
    };

    public static string MessageSessionInterfaceFullName => "NServiceBus.IMessageSession";

    // The transactional-outbox scope moved out of the `.TransactionalSession` sub-namespace and is now the generic
    // `IMessagingScope<TDbContext>` (arity 1) directly under `Chabis.Messaging.NServiceBus`. GetTypeByMetadataName
    // requires the `1 arity suffix for generic types. This is how outbox-published NServiceBus messages are detected.
    public static string MessagingScopeInterfaceFullName => "Chabis.Messaging.NServiceBus.IMessagingScope";

    public static string MessageSessionExtensionsFullName => "NServiceBus.MessageSessionExtensions";

    public const string MessageHandlerInterfaceName = "IHandleMessages";
    public const string MessageHandlerMethodName = "Handle";

    public static string MessageHandlerInterfaceFullNameWithArity => $"NServiceBus.{MessageHandlerInterfaceName}`1";
}

public static class KafkaConstants
{
    public static string[] ProduceMethodNames= new string[] { "ProduceAsync", "ProduceBatchAsync", "ProduceTombstoneAsync", "ProduceAndWaitForDeliveryResultAsync"};

    // NOTE: direct Kafka production uses IKafkaProducer<TKey, TMessage> (arity 2). The transactional Kafka outbox path,
    // however, wraps IKafkaProducer<TDbContext> (arity 1, e.g. IKafkaProducer<ProviderOrderDbContext>). To detect
    // outbox Kafka produces, FindKafkaCallsAsync would need to also scan the arity-1 producer (a code change, since this
    // constant is consumed as a single metadata name).
    public static string[] ProducerInterfaceFullNames => new string[] { "Chabis.EventStreaming.IKafkaProducer" };


    public const string BatchMessageHandlerInterfaceName = "IKafkaMessageBatchConsumer";
    public const string MessageHandlerInterfaceName = "IKafkaMessageConsumer";


    public const string MessageHandlerMethodName = "HandleAsync";
    public const string BatchMessageHandlerMethodName = "HandleBatchAsync";

    public static string[] MessageHandlerInterfaceFullNames => new string[]
    {
        $"Chabis.EventStreaming.{BatchMessageHandlerInterfaceName}",
        $"Chabis.EventStreaming.{MessageHandlerInterfaceName}",
    };
    public static string[] MessageHandlerInterfaceFullNamesWithArity => new string[]
    {
        $"Chabis.EventStreaming.{BatchMessageHandlerInterfaceName}`2",
        $"Chabis.EventStreaming.{MessageHandlerInterfaceName}`2",
    };
}

public static class ServiceClientConstants
{
    public static string ProviderOrderInterfaceServiceClientRootNamespace => $"Dg.ProviderOrderInterface.ServiceClient";
    public static string ProviderOrderServiceClientRootNamespace => $"Dg.ProviderOrder.ServiceClient";
}

public static class SourceCodeConstants
{
    public const string ProviderOrderContractsProjectName = "Dg.ProviderOrder.Contracts";
    public const string ProviderOrderProjectName = "Dg.ProviderOrder";
    public const string ProviderOrderApplicationNamespace = "Dg.ProviderOrder.Application";

    public const string MonolithErpWebsiteProjectName = "Dg.Erp.Website";

    public const string ErpControllerBaseClassFullName = "Chabis.Erp.ErpControllers.ErpController";
    public const string ChabisControllerBaseClassFullName = "Chabis.Website.Controllers.BaseController.BaseController";
    public const string AspNetCoreControllerBaseClassFullName = "Microsoft.AspNetCore.Mvc.ControllerBase";
    public const string ErpCrudControllerBaseClassFullName = "Chabis.Erp.ErpControllers.ErpCrudController";

    // NOTE: `Dg.OnlineShop.Framework.MonoLisoShopController` no longer exists in either code base. It is kept in the
    // list below only so IsController keeps working (a non-existent base name simply never matches, no error). Added
    // `Chabis.PartnerPortal.Controllers.PartnerPortalController`, a real base used by ProviderOrder controllers.
    // Caveat: IsController compares only the DIRECT base type. Controllers whose immediate base is an intermediate
    // in-repo class (e.g. EdiDataContentController -> ErpController, or PartnerPortalController -> ErpController) are
    // only caught if that intermediate base is listed here. Walking the full inheritance chain would be a more robust
    // fix (code change in RoslynDgExtensions.IsController).
    public const string MonoLisoShopControllerBaseClassFullName = "Dg.OnlineShop.Framework.MonoLisoShopController";
    public const string PartnerPortalControllerBaseClassFullName = "Chabis.PartnerPortal.Controllers.PartnerPortalController";

    public static readonly string[] AllControllerBaseClassFullNames = new string[]
    {
        ErpControllerBaseClassFullName,
        ChabisControllerBaseClassFullName,
        AspNetCoreControllerBaseClassFullName,
        ErpCrudControllerBaseClassFullName,
        MonoLisoShopControllerBaseClassFullName,
        PartnerPortalControllerBaseClassFullName
    };

    public static readonly string[] ErpViewFrameworkNamespaces = new string[]
    {
        "Chabis.Erp.Views",
        "Chabis.Website.Views"
    };

    // The old `Dg.Logistics.LogisticsFramework.Workflow.WorkflowManager` no longer exists. The current workflow
    // manager is the generic `Chabis.Workflow.OldAsDirtWorkflow.WorkflowManager<TWorkflowData, TState>`. IsWorkflow
    // uses StartsWith on the display string, so the namespace prefix (without arity) matches correctly.
    public const string LogisticsWorkflowManagerClassName = "Chabis.Workflow.OldAsDirtWorkflow.WorkflowManager";
    public const string PortalWorkflowMNamespace = "Chabis.Workflow.PortalWorkflow";

    // NOTE: no `OrderPositionModificationStrategyManager` type exists in the current code base. This value is retained
    // only so IsModificationStrategyManagerCall compiles; it will never match (StartsWith on a non-existent name).
    public const string OrderPositionModificationStrategyManagerProcessMethodFullName =
        "Dg.CategoryManagement.SalesOrder.OrderManager.OrderPositionModification.OrderPositionModificationStrategyManager.ProcessAsync";

    // `OrderModificationStrategyManager` moved to the OrderAccountingCore order-modification framework (it is now
    // internal and marked [Obsolete], but still callable and worth excluding from call stacks).
    public const string OrderModificationStrategyManagerProcessMethodFullName =
        "Dg.ErpOrderControllers.OrderAccountingCore.Orders.OrderModificationFramework.OrderModificationStrategyManager.ProcessAsync";


}