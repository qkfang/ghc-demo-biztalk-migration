using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SuperFundManagementFunc.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddScoped<IContributionTransformService, ContributionTransformService>();
        services.AddScoped<IFundAllocationSenderService, FundAllocationSenderService>();
    })
    .Build();

host.Run();
