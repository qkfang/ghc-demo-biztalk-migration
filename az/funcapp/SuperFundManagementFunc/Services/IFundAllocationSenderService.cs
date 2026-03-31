using System.Threading.Tasks;

namespace SuperFundManagement.Functions;

public interface IFundAllocationSenderService
{
    Task SendAsync(FundAllocationInstruction instruction);
}
