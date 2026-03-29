using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SuperFundManagement.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureOpenApi()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<IContributionTransformService, ContributionTransformService>();
        services.AddHttpClient<IFundAllocationSenderService, FundAllocationSenderService>(client =>
            client.BaseAddress = new Uri(
                context.Configuration["FundAdminApiUrl"]
                ?? throw new InvalidOperationException("FundAdminApiUrl is required")));
    })
    .Build();

await host.RunAsync();
