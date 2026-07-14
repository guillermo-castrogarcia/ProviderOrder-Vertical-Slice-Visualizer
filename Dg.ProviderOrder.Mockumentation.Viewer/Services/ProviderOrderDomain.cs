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
    /// Port of ProviderOrderExtensions.FindProviderOrderProduct(FullName). The product is the feature
    /// folder — the namespace segment right after ".Commands." / ".Queries." — so a product keyword that
    /// appears deeper in the name (e.g. "InvoicedButNotDeliveredCleanup" under a Cancellation feature)
    /// can't win. The ordering of the checks is significant and preserved from the original.
    /// </summary>
    public static string FindProduct(string fullNameValue)
    {
        var scope = ProductScope(fullNameValue);
        if (scope.Contains("OrderCreation")) return "OrderCreation";
        if (scope.Contains("OrderExport")) return "OrderExport";
        if (scope.Contains("ProviderResponse")) return "ProviderResponse";
        if (scope.Contains("ProviderDelivery")) return "ProviderDelivery";
        if (scope.Contains("Invoice")) return "ProviderInvoice";
        if (scope.Contains("Return")) return "Returns";
        if (scope.Contains("Cancellation")) return "Cancellation";
        if (scope.Contains("ProviderRuling")) return "ProviderRuling";
        return "Unknown";
    }

    // Returns the namespace segment immediately after ".Commands." or ".Queries." (the feature folder that
    // names the product), or the whole value if neither marker is present.
    private static string ProductScope(string fullNameValue)
    {
        foreach (var marker in new[] { ".Commands.", ".Queries." })
        {
            var start = fullNameValue.IndexOf(marker, System.StringComparison.Ordinal);
            if (start < 0) continue;
            start += marker.Length;
            var end = fullNameValue.IndexOf('.', start);
            return end < 0 ? fullNameValue[start..] : fullNameValue[start..end];
        }

        return fullNameValue;
    }
}
