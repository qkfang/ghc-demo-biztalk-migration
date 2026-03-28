# BizTalk OrderProcessing Solution

This folder contains the BizTalk Server 2020 solution for the **Order Processing** application — the legacy baseline for the migration demo.

## Solution Structure

```
OrderProcessing/
├── OrderProcessing.btproj         # BizTalk project file
├── BindingFile.xml                # Port & orchestration bindings
├── Schemas/
│   ├── SourceOrderSchema.xsd      # Incoming XML order schema
│   └── TargetFulfillmentSchema.xsd # Outgoing fulfillment schema
├── Maps/
│   └── OrderToFulfillmentMap.btm  # XSLT map with functoids
├── Orchestrations/
│   └── OrderProcessingOrchestration.odx  # Main orchestration
└── Pipelines/
    ├── HttpReceivePipeline.btp    # Receive pipeline (XML Disassembler)
    └── HttpSendPipeline.btp       # Send pipeline (XML Assembler)
```

## Prerequisites

- **BizTalk Server 2020** (Developer or Enterprise Edition)
- **Visual Studio 2019/2022** with BizTalk extensions installed
- **BizTalk Server 2020 Developer Tools** (included with BizTalk installation)
- SQL Server 2016+ (for BizTalk Management DB)
- IIS configured for HTTP adapter

## Build

1. Open `OrderProcessing.sln` in Visual Studio
2. Right-click the solution → **Restore NuGet Packages**
3. Set the `BizTalkInstallPath` property if not set automatically:
   ```xml
   <!-- In project properties or Directory.Build.props -->
   <BizTalkInstallPath>C:\Program Files (x86)\Microsoft BizTalk Server 2020</BizTalkInstallPath>
   ```
4. Build the solution:
   ```
   msbuild OrderProcessing.sln /p:Configuration=Release
   ```

## Deploy to BizTalk Server

### Option 1: Deploy from Visual Studio

1. In Solution Explorer, right-click `OrderProcessing` project
2. Select **Deploy**
3. The project auto-deploys to the BizTalk Management Database

### Option 2: Deploy via BTSTask

```powershell
# Import the MSI (after building)
BTSTask ImportApp /Package:"bin\Release\OrderProcessing.msi" /Overwrite

# Import bindings
BTSTask ImportBindings /ApplicationName:OrderProcessing /Source:BindingFile.xml

# Enlist and start
BTSTask StartApplication /ApplicationName:OrderProcessing
```

### Option 3: Deploy via BizTalk Admin Console

1. Open **BizTalk Server Administration Console**
2. Right-click **Applications** → **Import** → **MSI file**
3. Follow the import wizard
4. After import, right-click application → **Import Bindings**
5. Select `BindingFile.xml`
6. Start the application

## Port Configurations

### Receive Location: `OrderHttpReceive_Location`

| Property        | Value                              |
|-----------------|------------------------------------|
| Transport       | HTTP                               |
| URL             | `/OrderProcessing/Receive`         |
| Port            | 7070 (IIS binding)                 |
| Pipeline        | `HttpReceivePipeline`              |
| Message Type    | `SourceOrder.OrderRequest`         |
| Authentication  | None (extend for production)       |

The HTTP adapter is hosted in IIS. Configure an IIS application pointing to `%BTSHTTPRECEIVE%` virtual directory on port 7070.

### Send Port: `FulfillmentHttpSend`

| Property        | Value                                          |
|-----------------|------------------------------------------------|
| Transport       | HTTP                                           |
| URL             | `http://downstream-service/api/fulfillment`    |
| Pipeline        | `HttpSendPipeline`                             |
| Content-Type    | `application/xml`                              |
| Retry Count     | 3                                              |
| Retry Interval  | 5 seconds                                      |
| Map             | `OrderToFulfillmentMap`                        |
| Filter          | `BTS.ReceivePortName == OrderHttpReceive`      |

## Orchestration Flow

```
HTTP POST (XML OrderRequest)
        ↓
[OrderHttpReceive] Receive Port
        ↓
[HttpReceivePipeline] XML Disassembler + Validator
        ↓
[OrderProcessingOrchestration]
    1. ReceiveOrder (activate)
    2. Construct FulfillmentOrderMsg via OrderToFulfillmentMap
    3. SendFulfillmentOrder
        ↓
[HttpSendPipeline] XML Assembler
        ↓
[FulfillmentHttpSend] Send Port
        ↓
HTTP POST to downstream-service/api/fulfillment
```

## Map: OrderToFulfillmentMap

Key transformations:

| Source Field                | Target Field                     | Logic                          |
|-----------------------------|----------------------------------|--------------------------------|
| `OrderId`                   | `FulfillmentId`                  | String Concatenate: `"FF-"` + OrderId |
| `OrderId`                   | `SourceOrderRef`                 | Direct copy                    |
| `CustomerId`                | `CustomerDetails/Id`             | Direct copy                    |
| `CustomerName`              | `CustomerDetails/Name`           | Direct copy                    |
| `CustomerEmail`             | `CustomerDetails/Email`          | Direct copy                    |
| `OrderDate`                 | `RequestedDate`                  | Direct copy                    |
| `Items/Item[*]`             | `LineItems/LineItem[*]`          | Looping functoid               |
| `Item/ProductCode`          | `LineItem/SKU`                   | Direct copy                    |
| `Item/ProductName`          | `LineItem/Description`           | Direct copy                    |
| `Item/Quantity`             | `LineItem/Qty`                   | Direct copy                    |
| `Item/UnitPrice`            | `LineItem/Price`                 | Direct copy                    |
| `Item/Quantity * UnitPrice` | `LineItem/LineTotal`             | Multiplication functoid        |
| `TotalAmount`               | `OrderTotal`                     | Direct copy                    |
| `Currency`                  | `CurrencyCode`                   | Direct copy                    |
| `ShippingAddress/Street`    | `ShipTo/AddressLine1`            | Direct copy                    |
| `ShippingAddress/City`      | `ShipTo/City`                    | Direct copy                    |
| `ShippingAddress/State`     | `ShipTo/StateProvince`           | Direct copy                    |
| `ShippingAddress/ZipCode`   | `ShipTo/PostalCode`              | Direct copy                    |
| `ShippingAddress/Country`   | `ShipTo/CountryCode`             | Direct copy                    |
| *(constant)*                | `Status`                         | Value: `"PENDING"`             |

## Troubleshooting

- **Pipeline validation failure**: Ensure `SourceOrderSchema.xsd` is deployed to the BizTalk Management DB
- **HTTP 404 on receive**: Verify IIS virtual directory for HTTP adapter is running on port 7070
- **Orchestration suspended**: Check BizTalk Admin Console → Group Hub → Suspended Instances
- **Map failures**: Use BizTalk Mapper in Visual Studio to test the `.btm` file with sample XML
