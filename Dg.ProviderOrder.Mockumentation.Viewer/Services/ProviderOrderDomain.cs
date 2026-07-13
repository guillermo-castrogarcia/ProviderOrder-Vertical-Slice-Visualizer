namespace Dg.ProviderOrder.Mockumentation.Viewer.Services;

/// <summary>
/// Faithful port of the small pure helpers from Dg.ProviderOrder.Mokumentation
/// (ProviderOrderExtensions / FullName) that the viewer needs, so the host stays decoupled
/// from the analysis toolchain. Behaviour must match the originals.
/// </summary>
public static class ProviderOrderDomain
{
    /// <summary>Mirrors PrimaryAdapterType. Index matches the serialized int.</summary>
    public static string AdapterTypeName(int adapterType) => adapterType switch
    {
        0 => "Web",
        1 => "NServiceBus",
        2 => "Kafka",
        _ => "Other",
    };

    /// <summary>Mirrors ApplicationSide (Query = 0, Command = 1).</summary>
    public static string SideName(int applicationSide) => applicationSide == 1 ? "Command" : "Query";

    /// <summary>
    /// Port of ProviderOrderExtensions.FindProviderOrderProduct(FullName). The ordering of the
    /// checks is significant and preserved from the original.
    /// </summary>
    public static string FindProduct(string fullNameValue)
    {
        if (fullNameValue.Contains("OrderCreation")) return "OrderCreation";
        if (fullNameValue.Contains("OrderExport")) return "OrderExport";
        if (fullNameValue.Contains("ProviderResponse")) return "ProviderResponse";
        if (fullNameValue.Contains("ProviderDelivery")) return "ProviderDelivery";
        if (fullNameValue.Contains("Invoice")) return "ProviderInvoice";
        if (fullNameValue.Contains("CustomerReturn")) return "Returns";
        if (fullNameValue.Contains("Cancellation")) return "Cancellation";
        if (fullNameValue.Contains("ProviderRuling")) return "ProviderRuling";
        return "Unknown";
    }
}
