using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SuperFundManagement.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<IContributionTransformService, ContributionTransformService>();
        services.AddHttpClient<IFundAllocationSenderService, FundAllocationSenderService>(client =>
        {
            var url = context.Configuration["FundAdminApiUrl"]
                ?? throw new InvalidOperationException("FundAdminApiUrl app setting is required");
            client.BaseAddress = new Uri(url);
        });
    })
    .Build();

await host.RunAsync();
