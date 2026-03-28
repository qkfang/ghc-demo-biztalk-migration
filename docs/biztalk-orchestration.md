# BizTalk Orchestration: OrderProcessing

## Overview

The `OrderProcessingOrchestration` is a BizTalk Server 2020 orchestration that implements a one-way message flow. It receives an HTTP POST containing an XML `OrderRequest`, transforms it to a `FulfillmentOrder` XML document using a BizTalk map, and forwards the result to a downstream HTTP endpoint.

---

## Architecture

```
                          BizTalk Server 2020
  ┌─────────────┐         ┌─────────────────────────────────────────────────────────┐
  │             │  HTTP   │                                                         │
  │ HTTP Client │ ──POST─▶│  Receive Port         Orchestration      Send Port      │
  │             │  :7070  │  ┌──────────────┐     ┌────────────┐    ┌────────────┐ │
  └─────────────┘         │  │OrderHttp     │     │  Order     │    │Fulfillment │ │
                          │  │Receive       │────▶│  Processing│───▶│HttpSend    │ │
                          │  │              │     │  Orch.     │    │            │ │
                          │  └──────────────┘     └────────────┘    └──────┬─────┘ │
                          └────────────────────────────────────────────────┼────────┘
                                                                            │  HTTP POST
                                                                            ▼
                                                                  ┌─────────────────┐
                                                                  │ Downstream      │
                                                                  │ Fulfillment     │
                                                                  │ Service         │
                                                                  └─────────────────┘
```

---

## Receive Port Configuration

**Port Name:** `OrderHttpReceive`

| Property             | Value                              |
|----------------------|------------------------------------|
| Adapter              | HTTP                               |
| Direction            | Receive                            |
| URL                  | `/OrderProcessing/Receive`         |
| IIS Port             | 7070                               |
| Pipeline             | `HttpReceivePipeline`              |
| Message Schema       | `SourceOrder.OrderRequest`         |
| Authentication       | Anonymous (extend for production)  |
| Receive Handler      | `BizTalkServerApplication`        |

The HTTP receive adapter is hosted inside IIS via the `BTSHTTPReceive.dll` ISAPI extension. The IIS virtual directory must be configured on port 7070.

---

## Schema Descriptions

### Source Schema: `SourceOrderSchema.xsd`

Namespace: `http://OrderProcessing.Schemas.SourceOrder`  
Root Element: `OrderRequest`

| Field                        | Type       | Notes                              |
|------------------------------|------------|------------------------------------|
| `OrderId`                    | string     | Unique order identifier            |
| `CustomerId`                 | string     | Customer account number            |
| `CustomerName`               | string     | Full name                          |
| `CustomerEmail`              | string     | Contact email                      |
| `OrderDate`                  | dateTime   | ISO 8601 UTC                       |
| `Items/Item[+]`              | sequence   | One or more items                  |
| `Items/Item/ProductCode`     | string     | Product SKU                        |
| `Items/Item/ProductName`     | string     | Human-readable product name        |
| `Items/Item/Quantity`        | int        | Units ordered                      |
| `Items/Item/UnitPrice`       | decimal    | Price per unit                     |
| `TotalAmount`                | decimal    | Pre-calculated order total         |
| `Currency`                   | string     | Default: `USD`                     |
| `ShippingAddress/Street`     | string     | Street address                     |
| `ShippingAddress/City`       | string     |                                    |
| `ShippingAddress/State`      | string     | State/province code                |
| `ShippingAddress/ZipCode`    | string     | Postal code                        |
| `ShippingAddress/Country`    | string     | Country code (e.g., `US`)          |

### Target Schema: `TargetFulfillmentSchema.xsd`

Namespace: `http://OrderProcessing.Schemas.FulfillmentOrder`  
Root Element: `FulfillmentOrder`

| Field                             | Type       | Notes                              |
|-----------------------------------|------------|------------------------------------|
| `FulfillmentId`                   | string     | System-assigned, prefixed `FF-`    |
| `SourceOrderRef`                  | string     | Original OrderId for traceability  |
| `CustomerDetails/Id`              | string     |                                    |
| `CustomerDetails/Name`            | string     |                                    |
| `CustomerDetails/Email`           | string     |                                    |
| `RequestedDate`                   | dateTime   | Copied from `OrderDate`            |
| `LineItems/LineItem[+]`           | sequence   |                                    |
| `LineItems/LineItem/SKU`          | string     |                                    |
| `LineItems/LineItem/Description`  | string     |                                    |
| `LineItems/LineItem/Qty`          | int        |                                    |
| `LineItems/LineItem/Price`        | decimal    |                                    |
| `LineItems/LineItem/LineTotal`    | decimal    | Calculated: Qty × Price            |
| `OrderTotal`                      | decimal    |                                    |
| `CurrencyCode`                    | string     |                                    |
| `ShipTo/AddressLine1`             | string     |                                    |
| `ShipTo/City`                     | string     |                                    |
| `ShipTo/StateProvince`            | string     |                                    |
| `ShipTo/PostalCode`               | string     |                                    |
| `ShipTo/CountryCode`              | string     |                                    |
| `Status`                          | string     | Default: `PENDING`                 |

---

## Map: OrderToFulfillmentMap

**File:** `Maps/OrderToFulfillmentMap.btm`

### Transformation Rules

| Source Field                  | Target Field                     | Transformation Logic                          |
|-------------------------------|----------------------------------|-----------------------------------------------|
| `OrderId`                     | `FulfillmentId`                  | **String Concatenate Functoid**: `"FF-"` + `OrderId` |
| `OrderId`                     | `SourceOrderRef`                 | Direct link                                   |
| `CustomerId`                  | `CustomerDetails/Id`             | Direct link                                   |
| `CustomerName`                | `CustomerDetails/Name`           | Direct link                                   |
| `CustomerEmail`               | `CustomerDetails/Email`          | Direct link                                   |
| `OrderDate`                   | `RequestedDate`                  | Direct link                                   |
| `Items/Item` *(loop)*         | `LineItems/LineItem`             | **Looping Functoid**                          |
| `Item/ProductCode`            | `LineItem/SKU`                   | Direct link (within loop)                     |
| `Item/ProductName`            | `LineItem/Description`           | Direct link (within loop)                     |
| `Item/Quantity`               | `LineItem/Qty`                   | Direct link (within loop)                     |
| `Item/UnitPrice`              | `LineItem/Price`                 | Direct link (within loop)                     |
| `Item/Quantity × UnitPrice`   | `LineItem/LineTotal`             | **Multiplication Functoid** (within loop)     |
| `TotalAmount`                 | `OrderTotal`                     | Direct link                                   |
| `Currency`                    | `CurrencyCode`                   | Direct link                                   |
| `ShippingAddress/Street`      | `ShipTo/AddressLine1`            | Direct link                                   |
| `ShippingAddress/City`        | `ShipTo/City`                    | Direct link                                   |
| `ShippingAddress/State`       | `ShipTo/StateProvince`           | Direct link                                   |
| `ShippingAddress/ZipCode`     | `ShipTo/PostalCode`              | Direct link                                   |
| `ShippingAddress/Country`     | `ShipTo/CountryCode`             | Direct link                                   |
| *(constant)*                  | `Status`                         | **Constant Functoid**: value = `"PENDING"`    |

---

## Orchestration Flow

The orchestration is defined in `Orchestrations/OrderProcessingOrchestration.odx`.

### Step-by-Step

1. **Receive** – The orchestration activates when a message arrives on `ReceiveOrderPort`.
   - Message type: `OrderProcessing.Schemas.SourceOrder.OrderRequest`
   - Correlation set initialized on `OrderId` property

2. **Construct** – A `ConstructMessage` shape creates `FulfillmentOrderMsg`.
   - Contains a `Transform` shape that applies `OrderToFulfillmentMap`
   - Input: `OrderRequestMsg`
   - Output: `FulfillmentOrderMsg` (type `FulfillmentOrder`)

3. **Send** – The `SendFulfillmentOrder` shape publishes `FulfillmentOrderMsg`.
   - Bound to `SendFulfillmentPort`
   - Follows the existing correlation set

### Message Declarations

| Message Name          | Type                                          | Role     |
|-----------------------|-----------------------------------------------|----------|
| `OrderRequestMsg`     | `SourceOrder.OrderRequest`                    | Inbound  |
| `FulfillmentOrderMsg` | `FulfillmentOrder.FulfillmentOrder`           | Outbound |

---

## Send Port Configuration

**Port Name:** `FulfillmentHttpSend`

| Property        | Value                                          |
|-----------------|------------------------------------------------|
| Adapter         | HTTP                                           |
| URL             | `http://downstream-service/api/fulfillment`    |
| Content-Type    | `application/xml`                              |
| Pipeline        | `HttpSendPipeline`                             |
| Map             | `OrderToFulfillmentMap`                        |
| Retry Count     | 3                                              |
| Retry Interval  | 5 seconds                                      |
| Filter          | `BTS.ReceivePortName == OrderHttpReceive`      |

---

## Binding and Deployment Notes

1. **Build** the solution in Visual Studio (Release configuration).
2. **Deploy** using BTSTask or via Visual Studio Deploy context menu.
3. **Import bindings** from `BindingFile.xml`:
   ```powershell
   BTSTask ImportBindings /ApplicationName:OrderProcessing /Source:BindingFile.xml
   ```
4. **Start** the application from BizTalk Admin Console.
5. **Verify** the receive location is enabled and the IIS virtual directory is configured on port 7070.
6. **Test** by sending a sample XML POST to `http://localhost:7070/OrderProcessing/Receive`.
