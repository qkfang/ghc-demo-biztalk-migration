# SuperFund Function App

Azure Functions implementation of the BizTalk Server 2020 Superannuation Fund Management integration.

## Overview

This Function App migrates the BizTalk integration that:
1. Receives HTTP POST requests with XML `SuperContributionRequest` messages
2. Transforms them to `FundAllocationInstruction` format
3. Forwards the result to the downstream fund administration API

## BizTalk Mapping

| BizTalk Component | Azure Functions Equivalent |
|-------------------|----------------------------|
| `SuperContributionOrchestration.odx` | `SuperContributionFunction.cs` |
| `ContributionToAllocationMap.btm` | `TransformationService.cs` |
| `ContributionMapHelper.cs` | `TransformationService.FormatABN()` and `CalculateNetContribution()` |
| `HttpReceivePipeline.btp` | XML deserialization in Function |
| `HttpSendPipeline.btp` | XML serialization in Function |
| Receive Port (HTTP) | HTTP trigger binding |
| Send Port (HTTP) | HttpClient POST |

## Project Structure

```
az/funcapp/
├── SuperFundFunctionApp.csproj      # Main project file
├── Program.cs                       # Host configuration
├── host.json                        # Function host settings
├── local.settings.json              # Local development settings
├── Models/
│   ├── SuperContributionRequest.cs  # Input schema (BizTalk: SuperContributionSchema.xsd)
│   └── FundAllocationInstruction.cs # Output schema (BizTalk: FundAllocationSchema.xsd)
├── Services/
│   ├── ITransformationService.cs    # Service interface
│   └── TransformationService.cs     # Transformation logic (BizTalk map equivalent)
├── Functions/
│   └── SuperContributionFunction.cs # HTTP-triggered function
└── Tests/
    ├── SuperFundFunctionApp.Tests.csproj
    ├── Services/
    │   ├── TransformationServiceTests.cs
    │   └── TransformationIntegrationTests.cs
    └── TestData/
        ├── SampleInput.xml
        └── SampleOutput.xml
```

## Business Logic

### Transformation Rules (from BizTalk Map)

1. **AllocationId**: Concatenate "FA-" prefix with ContributionId
2. **ABN Formatting**: Format 11-digit ABN as "XX XXX XXX XXX"
   - Example: `51824753556` → `51 824 753 556`
3. **Net Contribution Calculation**: Apply 15% contributions tax
   - Formula: `NetAmount = GrossAmount × 0.85`
   - Example: `875.00` → `743.75`
4. **Status Constants**: Set to "PENDING" for allocation status

## API Endpoints

### POST /api/contributions

Process a superannuation contribution request.

**Request:**
- Content-Type: `application/xml`
- Body: SuperContributionRequest XML

**Response:**
- Content-Type: `application/xml`
- Body: AllocationAcknowledgement XML from fund admin platform

**Example Request:**

```bash
curl -X POST http://localhost:7071/api/contributions \
  -H "Content-Type: application/xml" \
  --data @Tests/TestData/SampleInput.xml
```

### GET /api/health

Health check endpoint for monitoring.

**Response:**
```json
{
  "status": "healthy",
  "service": "SuperFundFunctionApp",
  "timestamp": "2024-06-30T12:00:00Z"
}
```

## Local Development

### Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools v4
- Visual Studio 2022 or VS Code with Azure Functions extension

### Build and Run

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test

# Run the Function App locally
cd az/funcapp
func start
```

The Function App will be available at `http://localhost:7071`

### Configuration

Update `local.settings.json` with your settings:

```json
{
  "Values": {
    "FundAdminApiUrl": "http://localhost:5050/api/allocations"
  }
}
```

### Test with Sample Data

```bash
# Start the mock fund admin API (in separate terminal)
cd app-fundadmin
dotnet run

# Test the function with sample data
curl -X POST http://localhost:7071/api/contributions \
  -H "Content-Type: application/xml" \
  --data-binary @app-biztalk/SuperFundManagement/Samples/SampleInput.xml
```

## Testing

The project includes comprehensive unit and integration tests:

### Run All Tests

```bash
cd az/funcapp/Tests
dotnet test
```

### Test Coverage

- **TransformationServiceTests.cs**: Unit tests for FormatABN and CalculateNetContribution
- **TransformationIntegrationTests.cs**: End-to-end transformation tests with XML serialization

## Deployment

### Deploy to Azure

```bash
# Build and publish
dotnet publish --configuration Release

# Deploy using Azure Functions Core Tools
func azure functionapp publish <function-app-name>
```

### Deploy using Azure CLI

```bash
# Create a deployment package
cd az/funcapp
dotnet publish -c Release -o ./publish
cd publish
zip -r ../deploy.zip .

# Deploy to Azure
az functionapp deployment source config-zip \
  --resource-group rg-superfund-dev \
  --name <function-app-name> \
  --src ../deploy.zip
```

## Swagger/OpenAPI

The Function App includes OpenAPI documentation. After deployment, access the Swagger UI at:

```
https://<function-app-name>.azurewebsites.net/api/swagger/ui
```

## Monitoring

Application Insights automatically captures:
- Request telemetry
- Dependency tracking (HTTP calls to fund admin API)
- Exception logging
- Custom trace logs

View metrics in the Azure Portal under the Application Insights resource.

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `FUNCTIONS_WORKER_RUNTIME` | Runtime type | `dotnet-isolated` |
| `AzureWebJobsStorage` | Storage connection string | Required |
| `FundAdminApiUrl` | Fund admin API endpoint | `http://localhost:5050/api/allocations` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights connection | Set by Azure |

## Migration Benefits

Compared to BizTalk Server 2020:

✅ **Serverless**: Pay only for execution time, auto-scaling
✅ **Cloud-native**: No VM or server management required
✅ **DevOps-friendly**: CI/CD pipelines, automated deployments
✅ **Cost-effective**: Eliminate BizTalk licensing and infrastructure costs
✅ **Modern tooling**: Visual Studio, VS Code, standard .NET development
✅ **Observability**: Built-in Application Insights integration
✅ **Testability**: Standard xUnit tests, no special BizTalk test harness

## Known Limitations

- No built-in visual mapper (BizTalk Mapper replaced with C# code)
- No correlation sets (not needed for stateless HTTP request/response)
- No convoy patterns (not required for this simple integration)
- No long-running orchestrations (function timeout applies)

## Troubleshooting

### Function Not Starting

Check `host.json` and `local.settings.json` are present and valid.

### XML Deserialization Errors

Ensure the input XML matches the namespace: `http://SuperFundManagement.Schemas.SuperContribution`

### Fund Admin API Unreachable

Verify `FundAdminApiUrl` is configured correctly and the target service is running.

## License

This is a demonstration project for BizTalk to Azure Functions migration.
