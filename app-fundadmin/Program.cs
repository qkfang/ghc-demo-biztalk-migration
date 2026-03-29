using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// In-memory store for received allocations
var allocations = new List<AllocationRecord>();

// Health check
app.MapGet("/health", () =>
    Results.Ok(new { status = "healthy", service = "AllocationMockApi", timestamp = DateTime.UtcNow }));

// GET all received allocations as JSON
app.MapGet("/api/allocations", () => Results.Ok(allocations));

// Serve simple dashboard UI
app.MapGet("/", () => Results.Content(DashboardHtml.Page, "text/html"));

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

    // Store in memory for the dashboard
    allocations.Add(new AllocationRecord(
        allocationId, sourceRef, totalAllocated, currencyCode, memberCount, DateTime.UtcNow));

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

record AllocationRecord(
    string AllocationId,
    string SourceContributionRef,
    string TotalAllocated,
    string CurrencyCode,
    int MemberCount,
    DateTime ReceivedAt);

static class DashboardHtml
{
    public const string Page = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8"/>
<meta name="viewport" content="width=device-width,initial-scale=1"/>
<title>Fund Admin – Allocations Dashboard</title>
<style>
  *{box-sizing:border-box;margin:0;padding:0}
  body{font-family:system-ui,sans-serif;background:#f4f6f9;color:#1e293b;padding:2rem}
  h1{margin-bottom:.5rem}
  p.sub{color:#64748b;margin-bottom:1.5rem}
  table{width:100%;border-collapse:collapse;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 1px 3px rgba(0,0,0,.1)}
  th,td{text-align:left;padding:.75rem 1rem;border-bottom:1px solid #e2e8f0}
  th{background:#0f172a;color:#f8fafc;font-weight:600}
  tr:last-child td{border-bottom:none}
  tr:hover td{background:#f1f5f9}
  .empty{padding:2rem;text-align:center;color:#94a3b8}
  .badge{display:inline-block;background:#10b981;color:#fff;padding:.15rem .5rem;border-radius:4px;font-size:.8rem}
  #refresh{margin-bottom:1rem;padding:.5rem 1rem;border:none;background:#2563eb;color:#fff;border-radius:6px;cursor:pointer;font-size:.9rem}
  #refresh:hover{background:#1d4ed8}
</style>
</head>
<body>
<h1>Fund Admin – Received Allocations</h1>
<p class="sub">In-memory list of FundAllocationInstruction messages received by the mock API.</p>
<button id="refresh" onclick="load()">&#x21bb; Refresh</button>
<table>
  <thead><tr>
    <th>#</th><th>Allocation ID</th><th>Source Ref</th><th>Total</th><th>Currency</th><th>Members</th><th>Received At</th><th>Status</th>
  </tr></thead>
  <tbody id="rows"><tr><td colspan="8" class="empty">Loading…</td></tr></tbody>
</table>
<script>
async function load(){
  const rows=document.getElementById('rows');
  try{
    const res=await fetch('/api/allocations');
    const data=await res.json();
    if(!data.length){rows.innerHTML='<tr><td colspan="8" class="empty">No allocations received yet.</td></tr>';return;}
    rows.innerHTML=data.map((r,i)=>`<tr>
      <td>${i+1}</td>
      <td>${esc(r.allocationId)}</td>
      <td>${esc(r.sourceContributionRef)}</td>
      <td>${esc(r.totalAllocated)}</td>
      <td>${esc(r.currencyCode)}</td>
      <td>${r.memberCount}</td>
      <td>${new Date(r.receivedAt).toLocaleString()}</td>
      <td><span class="badge">ACCEPTED</span></td>
    </tr>`).join('');
  }catch(e){rows.innerHTML=`<tr><td colspan="8" class="empty">Error: ${esc(e.message)}</td></tr>`;}
}
function esc(s){const d=document.createElement('div');d.textContent=s||'';return d.innerHTML;}
load();
setInterval(load,5000);
</script>
</body>
</html>
""";
}
