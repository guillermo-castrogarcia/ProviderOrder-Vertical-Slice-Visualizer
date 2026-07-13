# Provider Order Component Queries
## ProviderInvoice
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderInvoice.EdiInvoice.UserInterface.EdiInvoiceController(EdiInvoiceController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderInvoice.EdiInvoice.GetEdiInvoiceRequestHandler(GetEdiInvoiceRequestHandler.HandleAsync):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderResponse
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderResponse.EdiOrderResponse.UserInterface.ImportedOrderResponseController(ImportedOrderResponseController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderResponse.EdiOrderResponse.GetImportedOrderResponseRequestHandler(GetImportedOrderResponseRequestHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
