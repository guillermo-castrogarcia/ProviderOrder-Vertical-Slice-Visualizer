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
        if (fullName.Value.Contains("OrderCreation"))
        {
            return ProviderOrderProduct.OrderCreation;
        }

        if (fullName.Value.Contains("OrderExport"))
        {
            return ProviderOrderProduct.OrderExport;
        }

        if (fullName.Value.Contains("ProviderResponse"))
        {
            return ProviderOrderProduct.ProviderResponse;
        }

        if (fullName.Value.Contains("ProviderDelivery"))
        {
            return ProviderOrderProduct.ProviderDelivery;
        }

        if (fullName.Value.Contains("Invoice"))
        {
            return ProviderOrderProduct.ProviderInvoice;
        }

        if (fullName.Value.Contains("CustomerReturn"))
        {
            return ProviderOrderProduct.Returns;
        }

        if (fullName.Value.Contains("Cancellation"))
        {
            return ProviderOrderProduct.Cancellation;
        }
        if (fullName.Value.Contains("ProviderRuling"))
        {
            return ProviderOrderProduct.ProviderRuling;
        }

        return ProviderOrderProduct.Unknown;
    }

    public static ProviderOrderProduct? FindProviderOrderProduct(this VerticalSlice verticalSlice)
    {
        return verticalSlice.PrimaryPort.FullName.FindProviderOrderProduct();
    }

    public static ApplicationSide GetApplicationSide(this FullName fullName) => fullName.Value.Contains(".Commands.") ? ApplicationSide.Command : ApplicationSide.Query;

    public static ApplicationSide GetApplicationSide(this VerticalSlice verticalSlice) => verticalSlice.PrimaryPort.FullName.GetApplicationSide();
}