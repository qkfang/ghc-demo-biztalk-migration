using SuperFundManagement.Functions.Models;

namespace SuperFundManagement.Functions.Services
{
    public interface IContributionTransformService
    {
        FundAllocationInstruction Transform(SuperContributionRequest request);
    }
}
