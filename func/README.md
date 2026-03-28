# Azure Functions – OrderProcessingFunc

Cloud-native replacement for the BizTalk `OrderProcessingOrchestration`, built on **Azure Functions v4** with **.NET 8 isolated worker**.

## Project Structure

```
func/
├── OrderProcessingFunc.sln
├── OrderProcessingFunc/
│   ├── OrderProcessingFunc.csproj
│   ├── Program.cs                         # Host builder + DI
│   ├── host.json
│   ├── local.settings.json
│   ├── Models/
│   │   ├── SourceOrder.cs                 # Deserializes incoming XML
│   │   └── FulfillmentOrder.cs            # Serializes outgoing XML
│   ├── Services/
│   │   ├── IOrderTransformService.cs
│   │   ├── OrderTransformService.cs       # Replaces OrderToFulfillmentMap.btm
│   │   ├── IFulfillmentSenderService.cs
│   │   └── FulfillmentSenderService.cs    # Replaces FulfillmentHttpSend port
│   └── Functions/
│       └── OrderProcessingFunction.cs     # HTTP trigger – replaces orchestration
└── OrderProcessingFunc.Tests/
    ├── OrderProcessingFunc.Tests.csproj
    ├── Services/
    │   ├── OrderTransformServiceTests.cs
    │   └── FulfillmentSenderServiceTests.cs
    └── Functions/
        └── OrderProcessingFunctionTests.cs
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (local storage emulator) or a real Azure Storage account

## Run Locally

1. **Start Azurite** (for local storage):
   ```bash
   azurite --silent &
   ```

2. **Run the function app**:
   ```bash
   cd func/OrderProcessingFunc
   func start
   ```
   The function will listen at `http://localhost:7071/api/orders`.

3. **Test with curl**:
   ```bash
   curl -X POST http://localhost:7071/api/orders \
     -H "Content-Type: application/xml" \
     -d @../../docs/sample-order.xml
   ```

   **Sample XML payload** (`sample-order.xml`):
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <OrderRequest xmlns="http://OrderProcessing.Schemas.SourceOrder">
     <OrderId>ORD-2024-001</OrderId>
     <CustomerId>CUST-42</CustomerId>
     <CustomerName>Jane Smith</CustomerName>
     <CustomerEmail>jane.smith@example.com</CustomerEmail>
     <OrderDate>2024-06-15T10:30:00Z</OrderDate>
     <Items>
       <Item>
         <ProductCode>SKU-WIDGET-PRO</ProductCode>
         <ProductName>Widget Pro</ProductName>
         <Quantity>3</Quantity>
         <UnitPrice>29.99</UnitPrice>
       </Item>
     </Items>
     <TotalAmount>89.97</TotalAmount>
     <Currency>USD</Currency>
     <ShippingAddress>
       <Street>123 Main Street</Street>
       <City>Springfield</City>
       <State>IL</State>
       <ZipCode>62701</ZipCode>
       <Country>US</Country>
     </ShippingAddress>
   </OrderRequest>
   ```

   **Expected response (202 Accepted)**:
   ```json
   {
     "fulfillmentId": "FF-ORD-2024-001",
     "sourceOrderRef": "ORD-2024-001",
     "status": "PENDING"
   }
   ```

## Run Tests

```bash
cd func
dotnet test OrderProcessingFunc.sln --verbosity normal
```

With code coverage:
```bash
dotnet test OrderProcessingFunc.sln \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```

## Environment Variables

| Variable               | Description                                      | Default (local)                           |
|------------------------|--------------------------------------------------|-------------------------------------------|
| `FulfillmentServiceUrl`| URL of the downstream fulfillment HTTP endpoint  | `http://localhost:7072/api/mock-fulfillment` |
| `AzureWebJobsStorage`  | Azure Storage connection string                  | `UseDevelopmentStorage=true`              |

## HTTP API

### POST /api/orders

Accepts an XML `OrderRequest` and returns a fulfillment confirmation.

**Request:**
- Method: `POST`
- Content-Type: `application/xml`
- Body: XML conforming to `SourceOrderSchema.xsd`

**Responses:**

| Status | Meaning                                          |
|--------|--------------------------------------------------|
| `202`  | Order accepted and forwarded to fulfillment      |
| `400`  | Validation failed (missing OrderId, empty items) |
| `502`  | Downstream fulfillment service unreachable/error |
| `500`  | Internal transformation error                   |

## Dependency Injection

```
OrderProcessingFunction
  ├── IOrderTransformService  → OrderTransformService
  └── IFulfillmentSenderService → FulfillmentSenderService
         └── IHttpClientFactory (registered via AddHttpClient())
```

## Configuration for Production

In Azure, set the following Application Settings:
```
FulfillmentServiceUrl = https://your-downstream-service/api/fulfillment
```
