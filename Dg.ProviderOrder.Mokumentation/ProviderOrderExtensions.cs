namespace Dg.ProviderOrder.Mokumentation;

using ArchitectureBricks;

public enum ProviderOrderProduct
{
    OrderCreation,
    OrderExport,
    ProviderResponse,
    ProviderDelivery,
    ProviderInvoice,
    Returns,
    Cancellation,
    ProviderRuling,
    Unknown
}

public static class ProviderOrderExtensions
{
    public static string GetName(this ProviderOrderProduct? providerOrderProduct) => providerOrderProduct?.ToString() ?? "Other";
    public static ProviderOrderProduct? FindProviderOrderProduct(this FullName fullName)
    {
        // The product is the feature folder — the namespace segment right after ".Commands." / ".Queries.".
        // Matching only that segment (not the whole name) prevents a product keyword that appears deeper in
        // the name from winning: e.g. "...Commands.Cancellation.MandatorCancellation.InvoicedButNotDelivered
        // Cleanup..." is Cancellation, not ProviderInvoice.
        var scope = ProductScope(fullName.Value);

        if (scope.Contains("OrderCreation"))
        {
            return ProviderOrderProduct.OrderCreation;
        }

        if (scope.Contains("OrderExport"))
        {
            return ProviderOrderProduct.OrderExport;
        }

        if (scope.Contains("ProviderResponse"))
        {
            return ProviderOrderProduct.ProviderResponse;
        }

        if (scope.Contains("ProviderDelivery"))
        {
            return ProviderOrderProduct.ProviderDelivery;
        }

        if (scope.Contains("Invoice"))
        {
            return ProviderOrderProduct.ProviderInvoice;
        }

        if (scope.Contains("Return"))
        {
            return ProviderOrderProduct.Returns;
        }

        if (scope.Contains("Cancellation"))
        {
            return ProviderOrderProduct.Cancellation;
        }
        if (scope.Contains("ProviderRuling"))
        {
            return ProviderOrderProduct.ProviderRuling;
        }

        return ProviderOrderProduct.Unknown;
    }

    // Returns the namespace segment immediately after ".Commands." or ".Queries." (the feature folder that
    // names the product), or the whole value if neither marker is present.
    private static string ProductScope(string fullNameValue)
    {
        foreach (var marker in new[] { ".Commands.", ".Queries." })
        {
            var start = fullNameValue.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0)
            {
                continue;
            }

            start += marker.Length;
            var end = fullNameValue.IndexOf('.', start);
            return end < 0 ? fullNameValue[start..] : fullNameValue[start..end];
        }

        return fullNameValue;
    }

    public static ProviderOrderProduct? FindProviderOrderProduct(this VerticalSlice verticalSlice)
    {
        return verticalSlice.PrimaryPort.FullName.FindProviderOrderProduct();
    }

    public static ApplicationSide GetApplicationSide(this FullName fullName) => fullName.Value.Contains(".Commands.") ? ApplicationSide.Command : ApplicationSide.Query;

    public static ApplicationSide GetApplicationSide(this VerticalSlice verticalSlice) => verticalSlice.PrimaryPort.FullName.GetApplicationSide();
}