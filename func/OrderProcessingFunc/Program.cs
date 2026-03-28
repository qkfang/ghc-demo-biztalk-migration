using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderProcessingFunc.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddScoped<IOrderTransformService, OrderTransformService>();
        services.AddScoped<IFulfillmentSenderService, FulfillmentSenderService>();
    })
    .Build();

host.Run();
