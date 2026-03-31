using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SuperFundManagement.Functions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddHttpClient<IFundAllocationSenderService, FundAllocationSenderService>();
        services.AddSingleton<IContributionTransformService, ContributionTransformService>();
    })
    .Build();

host.Run();
