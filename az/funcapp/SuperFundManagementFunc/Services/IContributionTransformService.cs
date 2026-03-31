namespace SuperFundManagement.Functions;

public interface IContributionTransformService
{
    FundAllocationInstruction Transform(SuperContributionRequest request);
}
