using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SuperFundManagement.Functions.Models;
using SuperFundManagement.Functions.Services;

namespace SuperFundManagement.Functions.Functions
{
    public class SuperContributionFunction
    {
        private readonly IContributionTransformService _transformService;
        private readonly IFundAllocationSenderService _senderService;
        private readonly ILogger<SuperContributionFunction> _logger;

        public SuperContributionFunction(
            IContributionTransformService transformService,
            IFundAllocationSenderService senderService,
            ILogger<SuperContributionFunction> logger)
        {
            _transformService = transformService;
            _senderService = senderService;
            _logger = logger;
        }

        [Function("SuperContribution")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post",
                         Route = "SuperFundManagement/Receive")] HttpRequestData req)
        {
            _logger.LogInformation("Received SuperContribution request");

            // 1. Deserialize incoming XML (replaces ActivationReceive shape)
            SuperContributionRequest request;
            try
            {
                var body = await req.ReadAsStringAsync();
                request = DeserializeXml<SuperContributionRequest>(body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize request body");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid XML payload");
                return bad;
            }

            // 2. Transform (replaces Transform shape + Map)
            FundAllocationInstruction instruction;
            try
            {
                instruction = _transformService.Transform(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to transform contribution request");
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Failed to transform request");
                return bad;
            }

            // 3. Send (replaces Send shape + Send Port)
            try
            {
                var upstream = await _senderService.SendAsync(instruction);
                upstream.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send allocation {AllocationId}",
                    instruction.AllocationId);
                var err = req.CreateResponse(HttpStatusCode.BadGateway);
                await err.WriteStringAsync("Downstream allocation service error");
                return err;
            }

            _logger.LogInformation("Allocation {AllocationId} accepted",
                instruction.AllocationId);

            var ok = req.CreateResponse(HttpStatusCode.Accepted);
            ok.Headers.Add("Content-Type", "application/json");
            await ok.WriteStringAsync(JsonSerializer.Serialize(new
            {
                instruction.AllocationId,
                instruction.Status,
                MemberCount = instruction.MemberAllocations?.Allocation?.Count ?? 0,
                instruction.TotalAllocated
            }));
            return ok;
        }

        private static T DeserializeXml<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(xml);
            return (T)serializer.Deserialize(reader)!;
        }
    }
}
