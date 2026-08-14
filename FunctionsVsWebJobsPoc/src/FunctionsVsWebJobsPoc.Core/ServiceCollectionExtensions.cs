using FunctionsVsWebJobsPoc.Core.Csv;
using FunctionsVsWebJobsPoc.Core.Data;
using FunctionsVsWebJobsPoc.Core.Processing;
using FunctionsVsWebJobsPoc.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FunctionsVsWebJobsPoc.Core;

/// <summary>
/// Central DI registration for everything shared between the Function App and the WebJob host,
/// so both hosts wire up identical services and only differ in their trigger entry points.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPocCoreServices(this IServiceCollection services, IConfiguration configuration, string sqlConnectionStringName = "sqldb")
    {
        var connectionString = configuration.GetConnectionString(sqlConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{sqlConnectionStringName}' was not found.");

        services.AddDbContext<PocDbContext>(options => options.UseSqlServer(connectionString));

        services.AddSingleton<ICsvRowParser, CsvRowParser>();
        services.AddScoped<IBlobRowRepository, BlobRowRepository>();
        services.AddScoped<IMessageDataRepository, MessageDataRepository>();
        services.AddScoped<IBlobRowProcessor, BlobRowProcessor>();
        services.AddScoped<IServiceBusMessageProcessor, ServiceBusMessageProcessor>();

        return services;
    }
}
