using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SuperFundFunctionApp.Models;
using SuperFundFunctionApp.Services;

namespace SuperFundFunctionApp.Functions;

/// <summary>
/// Azure Function that processes superannuation contribution requests.
/// Replaces BizTalk orchestration: SuperContributionOrchestration.odx
///
/// BizTalk flow:
/// 1. Receive on ContributionHttpReceive port (HTTP POST /SuperFundManagement/Receive)
/// 2. Transform using ContributionToAllocationMap
/// 3. Send on AllocationHttpSend port to fund admin platform
/// </summary>
public class SuperContributionFunction
{
    private readonly ILogger<SuperContributionFunction> _logger;
    private readonly ITransformationService _transformationService;
    private readonly HttpClient _httpClient;

    public SuperContributionFunction(
        ILogger<SuperContributionFunction> logger,
        ITransformationService transformationService)
    {
        _logger = logger;
        _transformationService = transformationService;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Processes a superannuation contribution request and forwards the allocation instruction
    /// to the downstream fund administration platform.
    ///
    /// Maps to BizTalk receive port: ContributionHttpReceive (POST /SuperFundManagement/Receive)
    /// </summary>
    [Function("ProcessContribution")]
    [OpenApiOperation(operationId: "ProcessContribution", tags: new[] { "SuperFund" }, Summary = "Process Superannuation Contribution", Description = "Receives a SuperContributionRequest XML and transforms it to a FundAllocationInstruction for the fund administration platform")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody(contentType: "application/xml", bodyType: typeof(string), Required = true, Description = "SuperContributionRequest XML payload")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/xml", bodyType: typeof(string), Description = "Successfully processed and forwarded to fund admin platform")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/xml", bodyType: typeof(string), Description = "Invalid XML or validation error")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/xml", bodyType: typeof(string), Description = "Processing or downstream system error")]
    public async Task<HttpResponseData> ProcessContribution(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "contributions")] HttpRequestData req)
    {
        _logger.LogInformation("ProcessContribution function triggered");

        try
        {
            // Step 1: Read and deserialize the XML request
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("Received XML payload ({Length} bytes)", requestBody.Length);

            SuperContributionRequest? contributionRequest;
            try
            {
                contributionRequest = DeserializeXml<SuperContributionRequest>(requestBody);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("XML deserialization failed: {Message}", ex.Message);
                return CreateErrorResponse(req, HttpStatusCode.BadRequest,
                    "INVALID_XML", $"Failed to parse SuperContributionRequest: {ex.Message}");
            }

            if (contributionRequest == null)
            {
                return CreateErrorResponse(req, HttpStatusCode.BadRequest,
                    "INVALID_REQUEST", "SuperContributionRequest cannot be null");
            }

            _logger.LogInformation("Processing ContributionId={ContributionId}, Members={MemberCount}",
                contributionRequest.ContributionId,
                contributionRequest.Members?.Member.Count ?? 0);

            // Step 2: Transform using the transformation service (replaces BizTalk map)
            FundAllocationInstruction allocationInstruction;
            try
            {
                allocationInstruction = _transformationService.Transform(contributionRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transformation failed for ContributionId={ContributionId}",
                    contributionRequest.ContributionId);
                return CreateErrorResponse(req, HttpStatusCode.InternalServerError,
                    "TRANSFORMATION_ERROR", $"Failed to transform request: {ex.Message}");
            }

            _logger.LogInformation("Transformed to AllocationId={AllocationId}",
                allocationInstruction.AllocationId);

            // Step 3: Serialize to XML
            string allocationXml = SerializeToXml(allocationInstruction);

            // Step 4: Forward to fund admin platform (replaces BizTalk send port)
            string fundAdminUrl = Environment.GetEnvironmentVariable("FundAdminApiUrl")
                ?? "http://localhost:5050/api/allocations";

            try
            {
                var content = new StringContent(allocationXml, Encoding.UTF8, "application/xml");
                var response = await _httpClient.PostAsync(fundAdminUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Fund admin platform returned {StatusCode}: {Error}",
                        response.StatusCode, errorBody);
                    return CreateErrorResponse(req, HttpStatusCode.BadGateway,
                        "DOWNSTREAM_ERROR",
                        $"Fund admin platform returned {response.StatusCode}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Successfully forwarded to fund admin platform. AllocationId={AllocationId}",
                    allocationInstruction.AllocationId);

                // Return the acknowledgement from the fund admin platform
                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                successResponse.Headers.Add("Content-Type", "application/xml; charset=utf-8");
                await successResponse.WriteStringAsync(responseBody);
                return successResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to forward to fund admin platform");
                return CreateErrorResponse(req, HttpStatusCode.BadGateway,
                    "DOWNSTREAM_UNREACHABLE",
                    $"Could not reach fund admin platform: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in ProcessContribution");
            return CreateErrorResponse(req, HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR", $"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Health check endpoint for monitoring
    /// </summary>
    [Function("HealthCheck")]
    [OpenApiOperation(operationId: "HealthCheck", tags: new[] { "Monitoring" }, Summary = "Health Check", Description = "Returns the health status of the function app")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Description = "Service is healthy")]
    public HttpResponseData HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString("{\"status\":\"healthy\",\"service\":\"SuperFundFunctionApp\",\"timestamp\":\""
            + DateTime.UtcNow.ToString("o") + "\"}");
        return response;
    }

    private static T? DeserializeXml<T>(string xml) where T : class
    {
        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StringReader(xml);
        return serializer.Deserialize(reader) as T;
    }

    private static string SerializeToXml<T>(T obj) where T : class
    {
        var serializer = new XmlSerializer(typeof(T));
        var settings = new XmlWriterSettings
        {
            Indent = false,
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        serializer.Serialize(xmlWriter, obj);
        return stringWriter.ToString();
    }

    private HttpResponseData CreateErrorResponse(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string errorCode,
        string message)
    {
        var errorXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ContributionError>
  <Status>{errorCode}</Status>
  <Message>{System.Security.SecurityElement.Escape(message)}</Message>
  <Timestamp>{DateTime.UtcNow:o}</Timestamp>
</ContributionError>";

        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/xml; charset=utf-8");
        response.WriteString(errorXml);
        return response;
    }
}
