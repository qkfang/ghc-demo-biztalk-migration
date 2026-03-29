using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using SuperFundManagement.Functions.Models;

namespace SuperFundManagement.Functions.Services;

public class FundAllocationSenderService : IFundAllocationSenderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FundAllocationSenderService> _logger;

    public FundAllocationSenderService(HttpClient httpClient, ILogger<FundAllocationSenderService> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    public async Task<HttpResponseMessage> SendAsync(FundAllocationInstruction instruction)
    {
        _logger.LogInformation("Sending allocation {AllocationId} to fund admin API", instruction.AllocationId);

        var serializer = new XmlSerializer(typeof(FundAllocationInstruction));
        using var sw   = new StringWriter();
        serializer.Serialize(sw, instruction);
        var xml        = sw.ToString();

        var content  = new StringContent(xml, Encoding.UTF8, "application/xml");
        var response = await _httpClient.PostAsync(string.Empty, content);

        _logger.LogInformation("Fund admin API responded with {StatusCode}", response.StatusCode);
        return response;
    }
}
