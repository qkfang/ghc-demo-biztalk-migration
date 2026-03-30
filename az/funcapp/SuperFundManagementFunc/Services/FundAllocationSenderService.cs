using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using SuperFundManagement.Functions.Models;

namespace SuperFundManagement.Functions.Services
{
    public class FundAllocationSenderService : IFundAllocationSenderService
    {
        private readonly HttpClient _http;
        private readonly ILogger<FundAllocationSenderService> _logger;

        public FundAllocationSenderService(HttpClient http,
            ILogger<FundAllocationSenderService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> SendAsync(FundAllocationInstruction instruction)
        {
            var xml = SerializeToXml(instruction);
            var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            _logger.LogInformation("Sending allocation {AllocationId} to fund admin API",
                instruction.AllocationId);
            return await _http.PostAsync(string.Empty, content);
        }

        private static string SerializeToXml<T>(T obj)
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);
            var serializer = new XmlSerializer(typeof(T));
            using var sw = new StringWriter();
            serializer.Serialize(sw, obj, ns);
            return sw.ToString();
        }
    }
}
