using FunctionsVsWebJobsPoc.Core;
using FunctionsVsWebJobsPoc.Core.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: false);
        config.AddEnvironmentVariables();
    })
    .ConfigureWebJobs(webJobsBuilder =>
    {
        webJobsBuilder.AddAzureStorageBlobs();
        webJobsBuilder.AddAzureStorageQueues();
        webJobsBuilder.AddServiceBus();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddPocCoreServices(context.Configuration);
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PocDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

await host.RunAsync();