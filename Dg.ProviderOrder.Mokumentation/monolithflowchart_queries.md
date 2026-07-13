# Provider Order Component Queries
## ProviderDelivery.DispatchNotificationFeature.EdiDispatchNotification.EdiDeliveryNoteViewModelService
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderDelivery.DispatchNotificationFeature.EdiDispatchNotification.UserInterface.EdiDeliveryNoteController(EdiDeliveryNoteController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderDelivery.DispatchNotificationFeature.EdiDispatchNotification.EdiDeliveryNoteViewModelService(EdiDeliveryNoteViewModelService.GetImportedDeliveryNoteViewModelAsync):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderInvoice.EdiInvoice.FindRelatedInvoicesAndOrdersRequestHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderInvoice.EdiInvoice.UserInterface.EdiInvoiceController(EdiInvoiceController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderInvoice.EdiInvoice.FindRelatedInvoicesAndOrdersRequestHandler(FindRelatedInvoicesAndOrdersRequestHandler.HandleAsync):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderInvoice.EdiInvoice.GetEdiInvoiceForCustomerEdiDataIdRequestHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderInvoice.EdiInvoice.UserInterface.ImportedInvoiceController(ImportedInvoiceController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderInvoice.EdiInvoice.GetEdiInvoiceForCustomerEdiDataIdRequestHandler(GetEdiInvoiceForCustomerEdiDataIdRequestHandler.HandleAsync):::primaryport
    Dg.ProviderOrder.Infrastructure.Queries.ProviderInvoice.EdiInvoice.UserInterface.ImportedInvoiceController(ImportedInvoiceController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderInvoice.EdiInvoice.GetEdiInvoiceForCustomerEdiDataIdRequestHandler(GetEdiInvoiceForCustomerEdiDataIdRequestHandler.HandleAsync):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderInvoice.EdiInvoice.GetEdiInvoiceRequestHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderInvoice.EdiInvoice.UserInterface.EdiInvoiceController(EdiInvoiceController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderInvoice.EdiInvoice.GetEdiInvoiceRequestHandler(GetEdiInvoiceRequestHandler.HandleAsync):::primaryport
    Dg.ProviderOrder.Infrastructure.Queries.ProviderInvoice.EdiInvoice.UserInterface.EdiInvoiceController(EdiInvoiceController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderInvoice.EdiInvoice.GetEdiInvoiceRequestHandler(GetEdiInvoiceRequestHandler.HandleAsync):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderResponse.EdiOrderResponse.GetImportedOrderResponseRequestHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderResponse.EdiOrderResponse.UserInterface.ImportedOrderResponseController(ImportedOrderResponseController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderResponse.EdiOrderResponse.GetImportedOrderResponseRequestHandler(GetImportedOrderResponseRequestHandler.HandleAsync):::primaryport
    Dg.ProviderOrder.Application.Queries.ProviderResponse.EdiOrderResponse.GetImportedOrderResponseRequestHandler(GetImportedOrderResponseRequestHandler.HandleAsync):::primaryport --- Dg.ProviderOrderInterface.ServiceClient.IProviderOrderInterfaceServiceClient.OrderResponseEdiData_GetDownloadedOrderResponseEdiDataAsync_WebApi(OrderResponseEdiData_GetDownloadedOrderResponseEdiDataAsync):::webapicall
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
