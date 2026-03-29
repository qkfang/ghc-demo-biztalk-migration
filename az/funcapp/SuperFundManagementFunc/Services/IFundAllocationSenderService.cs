using System.Net.Http;
using System.Threading.Tasks;
using SuperFundManagement.Functions.Models;

namespace SuperFundManagement.Functions.Services
{
    public interface IFundAllocationSenderService
    {
        Task<HttpResponseMessage> SendAsync(FundAllocationInstruction instruction);
    }
}
