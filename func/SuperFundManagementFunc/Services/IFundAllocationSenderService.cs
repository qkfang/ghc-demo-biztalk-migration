using SuperFundManagementFunc.Models;

namespace SuperFundManagementFunc.Services;

/// <summary>
/// Defines the contract for sending a fund allocation instruction to the downstream fund administration platform.
/// Replaces the BizTalk AllocationHttpSend send port.
/// </summary>
public interface IFundAllocationSenderService
{
    /// <summary>
    /// Serializes the fund allocation instruction as XML and POSTs it to the configured fund administration platform URL.
    /// </summary>
    /// <param name="allocation">The fund allocation instruction to send.</param>
    /// <returns>The HTTP response from the fund administration platform.</returns>
    Task<HttpResponseMessage> SendAsync(FundAllocation allocation);
}
