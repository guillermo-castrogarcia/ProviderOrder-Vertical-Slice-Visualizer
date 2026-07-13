# Provider Order Module Queries
## ProviderDelivery.WarehouseDeliveryNotePdf.GenerateWarehouseDeliveryNotePdfHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderDelivery.WarehouseDeliveryNotePdf.WarehouseDeliveryNotePdfController(WarehouseDeliveryNotePdfController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderDelivery.WarehouseDeliveryNotePdf.GenerateWarehouseDeliveryNotePdfHandler(GenerateWarehouseDeliveryNotePdfHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderProductIdentityFeature.GetProviderProductKeySamplesHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderProductIdentityFeature.WebApi.ProductIdController(ProductIdController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderProductIdentityFeature.GetProviderProductKeySamplesHandler(GetProviderProductKeySamplesHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderDelivery.DeliveryTrackingFeature.GetDeliveryTrackingInformationFromDeliveryIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderDelivery.DeliveryTrackingFeature.UserInterface.DeliveryTrackingInformationController(DeliveryTrackingInformationController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderDelivery.DeliveryTrackingFeature.GetDeliveryTrackingInformationFromDeliveryIdHandler(GetDeliveryTrackingInformationFromDeliveryIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderProductIdentityFeature.GetProviderProductKeySampleHotAnd30DaysHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderProductIdentityFeature.WebApi.ProductIdController(ProductIdController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderProductIdentityFeature.GetProviderProductKeySampleHotAnd30DaysHandler(GetProviderProductKeySampleHotAnd30DaysHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## Cancellation.ProviderRuling.CancellationMetrics.FindProviderCancellationMetricPositionsHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.Cancellation.ProviderRuling.CancellationMetrics.UserInterface.ProviderCancellationMetricsController(ProviderCancellationMetricsController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.Cancellation.ProviderRuling.CancellationMetrics.FindProviderCancellationMetricPositionsHandler(FindProviderCancellationMetricPositionsHandler.Handle):::primaryport
    Dg.ProviderOrder.Infrastructure.Queries.Cancellation.ProviderRuling.CancellationMetrics.UserInterface.ProviderCancellationMetricsController(ProviderCancellationMetricsController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.Cancellation.ProviderRuling.CancellationMetrics.FindProviderCancellationMetricPositionsHandler(FindProviderCancellationMetricPositionsHandler.Handle):::primaryport
    Dg.ProviderOrder.Infrastructure.Queries.Cancellation.ProviderRuling.CancellationMetrics.WebApi.DownloadMetricDrilldownController(DownloadMetricDrilldownController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.Cancellation.ProviderRuling.CancellationMetrics.FindProviderCancellationMetricPositionsHandler(FindProviderCancellationMetricPositionsHandler.Handle):::primaryport
    Dg.ProviderOrder.Infrastructure.Queries.Cancellation.ProviderRuling.CancellationMetrics.WebApi.DownloadMetricDrilldownController(DownloadMetricDrilldownController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.Cancellation.ProviderRuling.CancellationMetrics.FindProviderCancellationMetricPositionsHandler(FindProviderCancellationMetricPositionsHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderProductIdentityFeature.FindProductIdsHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderProductIdentityFeature.WebApi.ProductIdController(ProductIdController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderProductIdentityFeature.FindProductIdsHandler(FindProductIdsHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderOrderConfirmationFeature.GetProviderOrderConfirmationsHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderOrderConfirmationFeature.WebApi.ProviderOrderConfirmationController(ProviderOrderConfirmationController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderOrderConfirmationFeature.GetProviderOrderConfirmationsHandler(GetProviderOrderConfirmationsHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderOrderItemProductInformationFeature.FindProviderOrderItemProductInformationFromIdRequestHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderOrderItemProductInformationFeature.WebApi.ProviderOrderItemProductInformationController(ProviderOrderItemProductInformationController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderOrderItemProductInformationFeature.FindProviderOrderItemProductInformationFromIdRequestHandler(FindProviderOrderItemProductInformationFromIdRequestHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderOrderItemProductFeature.GetProviderOrderItemProductOverviewsHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderOrderItemProductFeature.UserInterface.ProviderOrderItemProductController(ProviderOrderItemProductController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderOrderItemProductFeature.GetProviderOrderItemProductOverviewsHandler(GetProviderOrderItemProductOverviewsHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderOrderItemProductInformationFeature.FindProviderOrderItemProductInformationFromReferenceRequestHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderOrderItemProductInformationFeature.WebApi.ProviderOrderItemProductInformationController(ProviderOrderItemProductInformationController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderOrderItemProductInformationFeature.FindProviderOrderItemProductInformationFromReferenceRequestHandler(FindProviderOrderItemProductInformationFromReferenceRequestHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderOrderItemProductFeature.GetProviderOrderItemProductDetailHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderOrderItemProductFeature.UserInterface.ProviderOrderItemProductController(ProviderOrderItemProductController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderOrderItemProductFeature.GetProviderOrderItemProductDetailHandler(GetProviderOrderItemProductDetailHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderProductIdentityFeature.FindProviderProductIdentityHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderProductIdentityFeature.WebApi.ProviderProductIdentityController(ProviderProductIdentityController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderProductIdentityFeature.FindProviderProductIdentityHandler(FindProviderProductIdentityHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderBacklog.WarehouseDelivery.HasOpenBacklogHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderBacklog.WarehouseDelivery.ProviderOrderWarehouseBacklogController(ProviderOrderWarehouseBacklogController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderBacklog.WarehouseDelivery.HasOpenBacklogHandler(HasOpenBacklogHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## PurchasePriceForCustomerItemFeature.GetProviderOrderConfirmationsHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.PurchasePriceForCustomerItemFeature.WebApi.PurchasePriceForCustomerItemController(PurchasePriceForCustomerItemController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.PurchasePriceForCustomerItemFeature.GetProviderOrderConfirmationsHandler(GetProviderOrderConfirmationsHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderOrderItemProductForDirectDeliveryCustomerItemFeature.GetProviderOrderConfirmationsHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderOrderItemProductForDirectDeliveryCustomerItemFeature.WebApi.ProviderOrderItemProductForCustomerItemController(ProviderOrderItemProductForCustomerItemController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderOrderItemProductForDirectDeliveryCustomerItemFeature.GetProviderOrderConfirmationsHandler(GetProviderOrderConfirmationsHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## PurchaseDateForCustomerItemFeature.GetPurchaseDatesForCustomerItemsHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.PurchaseDateForCustomerItemFeature.WebApi.PurchaseDateForCustomerItemController(PurchaseDateForCustomerItemController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.PurchaseDateForCustomerItemFeature.GetPurchaseDatesForCustomerItemsHandler(GetPurchaseDatesForCustomerItemsHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderDelivery.DeliveryTrackingFeature.GetDeliveryTrackingInformationFromProviderOrderItemProductIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.ProviderDelivery.DeliveryTrackingFeature.UserInterface.DeliveryTrackingInformationForProviderOrderItemProductComponent(DeliveryTrackingInformationForProviderOrderItemProductComponent):::primaryadapter --- Dg.ProviderOrder.Application.Queries.ProviderDelivery.DeliveryTrackingFeature.GetDeliveryTrackingInformationFromProviderOrderItemProductIdHandler(GetDeliveryTrackingInformationFromProviderOrderItemProductIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## Cancellation.ProviderRuling.ImprovementPlan.CreateAndUpdateImprovementPlan.GetCreateImprovementPlanViewDataHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.Cancellation.ProviderRuling.CreateAndUpdateImprovementPlan.UserInterface.ProviderImprovementPlanController(ProviderImprovementPlanController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.Cancellation.ProviderRuling.ImprovementPlan.CreateAndUpdateImprovementPlan.GetCreateImprovementPlanViewDataHandler(GetCreateImprovementPlanViewDataHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## Cancellation.ProviderRuling.ImprovementPlan.RespondToImprovementPlan.GetRecentImprovementPlansWithPhasesHandler
```mermaid
flowchart LR
    Dg.ProviderOrder.Infrastructure.Queries.Cancellation.ProviderRuling.RespondToImprovementPlan.UserInterface.ImprovementPlanResponseController(ImprovementPlanResponseController):::primaryadapter --- Dg.ProviderOrder.Application.Queries.Cancellation.ProviderRuling.ImprovementPlan.RespondToImprovementPlan.GetRecentImprovementPlansWithPhasesHandler(GetRecentImprovementPlansWithPhasesHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ConextradeInvoiceFeature.HasConextradeInvoiceHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.ConextradeInvoiceFeature.WebApi.ConextradeInvoiceController(ConextradeInvoiceController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.ConextradeInvoiceFeature.HasConextradeInvoiceHandler(HasConextradeInvoiceHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderDelivery.DispatchNotification.EdiDeliveryNote.GetImportedDeliveryNoteFromDeliveryNoteEdiDataIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Import.ProviderDelivery.EdiDeliveryNote.WebApi.ImportedDeliveryNoteController(ImportedDeliveryNoteController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Import.ProviderDelivery.DispatchNotification.EdiDeliveryNote.GetImportedDeliveryNoteFromDeliveryNoteEdiDataIdHandler(GetImportedDeliveryNoteFromDeliveryNoteEdiDataIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## OrderExportResultFeature.FindOrderExportResultByIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Export.OrderExportFeature.UserInterface.OrderExportResultController(OrderExportResultController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Export.OrderExportResultFeature.FindOrderExportResultByIdHandler(FindOrderExportResultByIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderResponse.EdiOrderResponse.FindOrderResponseEdiContentHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Import.ProviderResponse.EdiOrderResponse.UserInterface.OrderResponseEdiContentController(OrderResponseEdiContentController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Import.ProviderResponse.EdiOrderResponse.FindOrderResponseEdiContentHandler(FindOrderResponseEdiContentHandler.Handle):::primaryport
    Dg.ProviderOrderInterface.Infrastructure.Queries.Import.ProviderResponse.EdiOrderResponse.UserInterface.OrderResponseEdiContentController(OrderResponseEdiContentController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Import.ProviderResponse.EdiOrderResponse.FindOrderResponseEdiContentHandler(FindOrderResponseEdiContentHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## CustomerReturnRegistrationExportResultFeature.FindCustomerReturnRegistrationExportResultPreviewsByOrderIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Export.CustomerReturnRegistrationExportFeature.WebApi.CustomerReturnRegistrationExportResultController(CustomerReturnRegistrationExportResultController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Export.CustomerReturnRegistrationExportResultFeature.FindCustomerReturnRegistrationExportResultPreviewsByOrderIdHandler(FindCustomerReturnRegistrationExportResultPreviewsByOrderIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## OrderExportFeature.EmailOrder.GetMailtoUriRequestHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Export.OrderExportFeature.EmailOrder.ManualEmailOrderExportController(ManualEmailOrderExportController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Export.OrderExportFeature.EmailOrder.GetMailtoUriRequestHandler(GetMailtoUriRequestHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderInvoice.EdiInvoice.GetConextradeAttachmentRequestHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Import.ProviderInvoice.EdiInvoice.WebApi.ConextradeAttachmentController(ConextradeAttachmentController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Import.ProviderInvoice.EdiInvoice.GetConextradeAttachmentRequestHandler(GetConextradeAttachmentRequestHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## CancelRequestExportResultFeature.FindCancelRequestExportResultPreviewsByOrderIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Export.CancelRequestExportFeature.WebApi.CancelRequestExportResultController(CancelRequestExportResultController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Export.CancelRequestExportResultFeature.FindCancelRequestExportResultPreviewsByOrderIdHandler(FindCancelRequestExportResultPreviewsByOrderIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## OrderExportFeature.FindOrderExportEdiDataContentByIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Export.OrderExportFeature.WebApi.OrderExportEdiDataContentController(OrderExportEdiDataContentController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Export.OrderExportFeature.FindOrderExportEdiDataContentByIdHandler(FindOrderExportEdiDataContentByIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## OrderExportResultFeature.CountOrderExportsByOrderIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Export.OrderExportFeature.WebApi.OrderExportCountController(OrderExportCountController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Export.OrderExportResultFeature.CountOrderExportsByOrderIdHandler(CountOrderExportsByOrderIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## CancelRequestExportFeature.FindCancelRequestExportEdiDataContentByIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Export.CancelRequestExportFeature.WebApi.CancelRequestExportEdiDataContentController(CancelRequestExportEdiDataContentController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Export.CancelRequestExportFeature.FindCancelRequestExportEdiDataContentByIdHandler(FindCancelRequestExportEdiDataContentByIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderResponse.EdiOrderResponse.GetDownloadedOrderResponseEdiDataHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Import.ProviderResponse.EdiOrderResponse.WebApi.OrderResponseEdiDataController(OrderResponseEdiDataController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Import.ProviderResponse.EdiOrderResponse.GetDownloadedOrderResponseEdiDataHandler(GetDownloadedOrderResponseEdiDataHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## CustomerReturnRegistrationExportResultFeature.FindCustomerReturnRegistrationExportResultByIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Export.CustomerReturnRegistrationExportFeature.UserInterface.CustomerReturnRegistrationExportResultController(CustomerReturnRegistrationExportResultController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Export.CustomerReturnRegistrationExportResultFeature.FindCustomerReturnRegistrationExportResultByIdHandler(FindCustomerReturnRegistrationExportResultByIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## GetEdiInterfaceProvidersFeature.GetEdiInterfaceProvidersHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.GetEdiInterfaceProvidersFeature.WebApi.EdiInterfaceProvidersController(EdiInterfaceProvidersController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.GetEdiInterfaceProvidersFeature.GetEdiInterfaceProvidersHandler(GetEdiInterfaceProvidersHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderResponse.EdiOrderResponse.GetImportedOrderResponseHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Import.ProviderResponse.EdiOrderResponse.WebApi.ImportedOrderResponseController(ImportedOrderResponseController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Import.ProviderResponse.EdiOrderResponse.GetImportedOrderResponseHandler(GetImportedOrderResponseHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## CancelRequestExportResultFeature.FindCancelRequestExportResultByIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Export.CancelRequestExportFeature.UserInterface.CancelRequestExportResultController(CancelRequestExportResultController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Export.CancelRequestExportResultFeature.FindCancelRequestExportResultByIdHandler(FindCancelRequestExportResultByIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## OrderExportResultFeature.FindOrderExportResultPreviewsByOrderIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Export.OrderExportFeature.WebApi.OrderExportResultController(OrderExportResultController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Export.OrderExportResultFeature.FindOrderExportResultPreviewsByOrderIdHandler(FindOrderExportResultPreviewsByOrderIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## ProviderInvoice.EdiInvoice.EdiInvoiceAdditionalInformationRequestHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Import.ProviderInvoice.EdiInvoice.WebApi.EdiInvoiceAdditionalInformationController(EdiInvoiceAdditionalInformationController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Import.ProviderInvoice.EdiInvoice.EdiInvoiceAdditionalInformationRequestHandler(EdiInvoiceAdditionalInformationRequestHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## CustomerReturnRegistrationExportFeature.FindCustomerReturnRegistrationExportEdiDataContentByIdHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.Export.CustomerReturnRegistrationExportFeature.WebApi.CustomerReturnRegistrationExportEdiDataContentController(CustomerReturnRegistrationExportEdiDataContentController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.Export.CustomerReturnRegistrationExportFeature.FindCustomerReturnRegistrationExportEdiDataContentByIdHandler(FindCustomerReturnRegistrationExportEdiDataContentByIdHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
## GetDgOpenTransConfigurationsFeature.GetProviderOrderConfirmationsHandler
```mermaid
flowchart LR
    Dg.ProviderOrderInterface.Infrastructure.Queries.GetDgOpenTransConfigurationsFeature.WebApi.DgOpenTransConfigurationController(DgOpenTransConfigurationController):::primaryadapter --- Dg.ProviderOrderInterface.Application.Queries.GetDgOpenTransConfigurationsFeature.GetProviderOrderConfirmationsHandler(GetProviderOrderConfirmationsHandler.Handle):::primaryport
    classDef primaryport fill:#AA3333
    classDef primaryadapter fill:#777700
    classDef nservicebuspayload fill:#AAAA33
```
