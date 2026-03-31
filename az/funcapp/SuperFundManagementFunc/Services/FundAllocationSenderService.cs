using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SuperFundManagement.Functions;

public class FundAllocationSenderService : IFundAllocationSenderService
{
    private readonly HttpClient _httpClient;
    private readonly string _fundAdminApiUrl;
    private readonly ILogger<FundAllocationSenderService> _logger;

    public FundAllocationSenderService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<FundAllocationSenderService> logger)
    {
        _httpClient = httpClient;
        _fundAdminApiUrl = configuration["FundAdminApiUrl"] ?? throw new InvalidOperationException("FundAdminApiUrl configuration is missing.");
        _logger = logger;
    }

    public async Task SendAsync(FundAllocationInstruction instruction)
    {
        _logger.LogInformation("Sending allocation {AllocationId} to fund admin", instruction.AllocationId);

        var xml = SerializeToXml(instruction);
        var content = new StringContent(xml, Encoding.UTF8, "application/xml");

        var response = await _httpClient.PostAsync($"{_fundAdminApiUrl}/api/allocations", content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Fund admin returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new HttpRequestException($"Fund admin API returned {(int)response.StatusCode}: {body}");
        }

        _logger.LogInformation("Allocation {AllocationId} sent successfully", instruction.AllocationId);
    }

    private static string SerializeToXml(FundAllocationInstruction instruction)
    {
        var serializer = new XmlSerializer(typeof(FundAllocationInstruction));
        using var writer = new StringWriter();
        serializer.Serialize(writer, instruction);
        return writer.ToString();
    }
}
