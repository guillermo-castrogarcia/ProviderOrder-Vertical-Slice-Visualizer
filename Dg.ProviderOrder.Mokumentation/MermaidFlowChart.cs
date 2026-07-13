using System.Text;

namespace Dg.ProviderOrder.Mokumentation;

using ArchitectureBricks;
using Microsoft.CodeAnalysis;

public abstract record FlowChartNode()
{
}

public record FlowChartPrimaryAdapter(PrimaryAdapter Adapter) : FlowChartNode()
{
    public override string ToString()
    {
        var adapterNode = $"{Adapter.FullName.HtmlEncodedValue}({Adapter.ClassTypeSymbol.Name}):::primaryadapter";
        if (Adapter.PayloadType != null)
        {
            var payloadNode = $"{Adapter.PayloadType.FullName.HtmlEncodedValue}({Adapter.PayloadType.Name}):::primaryadapterpayload";
            return $"{MermaidFlowChartFactory.MermaidFlowchartIndent}{payloadNode} {MermaidFlowChartFactory.DefaultNodeLink} {adapterNode}";
        }

        return adapterNode;
    }

    public static string ClassDefinition => "classDef primaryadapter fill:#777700";

    public static string PayloadClassDefinition => "classDef primaryadapterpayload fill:#AAAA00";
}

public record FlowChartPrimaryPort(PrimaryPort PrimaryPort) : FlowChartNode()
{
    public override string ToString() => $"{PrimaryPort.FullName.HtmlEncodedValue}({PrimaryPort.ImplementationClass.Name}.{PrimaryPort.ImplementationMethod.Name}):::primaryport";

    public static string ClassDefinition => "classDef primaryport fill:#AA3333";
}

public record FlowChartNServiceBusPayload(NamedTypeSymbol NamedTypeSymbol) : FlowChartNode()
{
    public override string ToString() => $"{NamedTypeSymbol.FullName.HtmlEncodedValue}_NServiceBus({NamedTypeSymbol.Name}):::nservicebuspayload";

    public static string ClassDefinition => "classDef nservicebuspayload fill:#AAAA33";
}

public record FlowChartKafkaPayload(NamedTypeSymbol NamedTypeSymbol) : FlowChartNode()
{
    public override string ToString() => $"{NamedTypeSymbol.FullName.HtmlEncodedValue}_Kafka({NamedTypeSymbol.Name}):::kafkapayload";

    public static string ClassDefinition => "classDef kafkapayload fill:#AA2233";
}

public record FlowChartWebApiServiceClientCall(NamedMethodSymbol Method, MethodSignature MethodSignature) : FlowChartNode()
{
    public override string ToString() => $"{Method.FullName.Value}_WebApi({Method.Name}):::webapicall";

    public static string ClassDefinition => "classDef webapicall fill:#77AA99";
}

public record MermaidFlowChart(VerticalSlice VerticalSlice, string Text);

public static class MermaidFlowChartFactory
{
    public const string MermaidFlowchartIndent = "    ";

    public const string DefaultNodeLink = "---";

    public static MermaidFlowChart CreateVerticalSliceMermaidFlowchart(
        VerticalSlice verticalSlice)
    {
        var builder = new StringBuilder();
        builder.AppendLine("flowchart LR");

        var primaryAdapterNodes = verticalSlice.PrimaryAdapters.Select(e => new FlowChartPrimaryAdapter(e));
        var primaryPortNode = new FlowChartPrimaryPort(verticalSlice.PrimaryPort);

        foreach (var primaryAdapterNode in primaryAdapterNodes)
        {
            builder.AppendLine(GetMermaidFlowChartNodeLinkLine(primaryAdapterNode, primaryPortNode));
        }

        foreach (var payload in verticalSlice.NServiceBusPayloads)
        {
            builder.AppendLine(GetMermaidFlowChartNodeLinkLine(primaryPortNode, new FlowChartNServiceBusPayload(payload)));
        }
        foreach (var payload in verticalSlice.KafkaPayloads)
        {
            builder.AppendLine(GetMermaidFlowChartNodeLinkLine(primaryPortNode, new FlowChartKafkaPayload(payload)));
        }
        foreach (var webApiServiceClientCall in verticalSlice.WebApiServiceClientCalls)
        {
            builder.AppendLine(GetMermaidFlowChartNodeLinkLine(primaryPortNode, new FlowChartWebApiServiceClientCall(webApiServiceClientCall.Method, webApiServiceClientCall.Signature)));
        }

        builder.AppendLine($"{MermaidFlowchartIndent}{FlowChartPrimaryPort.ClassDefinition}");
        builder.AppendLine($"{MermaidFlowchartIndent}{FlowChartPrimaryAdapter.ClassDefinition}");
        builder.AppendLine($"{MermaidFlowchartIndent}{FlowChartNServiceBusPayload.ClassDefinition}");

        return new MermaidFlowChart(verticalSlice, builder.ToString());
    }

    private static string GetMermaidFlowChartNodeLinkLine(FlowChartNode lhs, FlowChartNode rhs, string link = DefaultNodeLink) =>
        $"{MermaidFlowchartIndent}{lhs} {link} {rhs}";

    private static string GetMermaidFlowChartNodeLinkLine(FlowChartNode lhs, IEnumerable<FlowChartNode> rhs, string link = DefaultNodeLink)
    {
        var rhsString = string.Join(" & ", rhs.Select(n => n.ToString()));
        return $"{MermaidFlowchartIndent}{lhs} {link} {rhsString}";
    }

    private static string GetMermaidFlowChartNodeLinkLine(IEnumerable<FlowChartNode> lhs, FlowChartNode rhs, string link = DefaultNodeLink)
    {
        var lhsString = string.Join(" & ", lhs.Select(n => n.ToString()));
        return $"{MermaidFlowchartIndent}{lhsString} {link} {rhs}";
    }

    private static string GetMermaidFlowChartNodeLinkLine(
        IEnumerable<FlowChartNode> lhs,
        IEnumerable<FlowChartNode> rhs,
        string link = "---")
    {
        var lhsString = string.Join(" & ", lhs.Select(n => n.ToString()));
        var rhsString = string.Join(" & ", rhs.Select(n => n.ToString()));
        return $"{MermaidFlowchartIndent}{lhsString} {link} {rhsString}";
    }
}