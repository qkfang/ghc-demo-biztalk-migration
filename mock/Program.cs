using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Health check
app.MapGet("/health", () =>
    Results.Ok(new { status = "healthy", service = "AllocationMockApi", timestamp = DateTime.UtcNow }));

// Mock Fund Admin Platform – receives FundAllocationInstruction XML from BizTalk
app.MapPost("/api/allocations", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    logger.LogInformation("POST /api/allocations received ({Bytes} bytes)", body.Length);

    // Parse incoming FundAllocationInstruction XML
    XDocument doc;
    try
    {
        doc = XDocument.Parse(body);
    }
    catch (Exception ex)
    {
        logger.LogWarning("XML parse failed: {Message}", ex.Message);
        var errorXml = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("AllocationError",
                new XElement("Status", "REJECTED"),
                new XElement("Message", $"Invalid XML payload: {ex.Message}"),
                new XElement("ReceivedAt", DateTime.UtcNow.ToString("o"))
            )
        );
        return Results.Content(errorXml.ToString(), "application/xml", System.Text.Encoding.UTF8, StatusCodes.Status400BadRequest);
    }

    // Support both namespaced (from BizTalk) and non-namespaced XML
    XNamespace ns = "http://SuperFundManagement.Schemas.FundAllocation";
    var root = doc.Root;

    string Get(string name) =>
        root?.Element(ns + name)?.Value
        ?? root?.Element(name)?.Value
        ?? string.Empty;

    var allocationId   = Get("AllocationId");
    var sourceRef      = Get("SourceContributionRef");
    var totalAllocated = Get("TotalAllocated");
    var currencyCode   = Get("CurrencyCode");

    var memberAllocations = root?.Element(ns + "MemberAllocations")
                         ?? root?.Element("MemberAllocations");
    var memberCount = memberAllocations?.Elements().Count() ?? 0;

    logger.LogInformation(
        "Processed AllocationId={AllocationId}, Members={Count}, Total={Total} {Currency}",
        allocationId, memberCount, totalAllocated, currencyCode);

    // Build AllocationAcknowledgement response, echoing key fields from the request
    var response = new XDocument(
        new XDeclaration("1.0", "utf-8", null),
        new XElement("AllocationAcknowledgement",
            new XElement("AllocationId", allocationId),
            new XElement("SourceContributionRef", sourceRef),
            new XElement("Status", "ACCEPTED"),
            new XElement("ProcessedAt", DateTime.UtcNow.ToString("o")),
            new XElement("AllocationsProcessed", memberCount),
            new XElement("TotalAllocated", totalAllocated),
            new XElement("CurrencyCode", currencyCode),
            new XElement("Message", "Fund allocation instruction received and queued for processing.")
        )
    );

    return Results.Content(response.ToString(), "application/xml");
});

app.Run();
