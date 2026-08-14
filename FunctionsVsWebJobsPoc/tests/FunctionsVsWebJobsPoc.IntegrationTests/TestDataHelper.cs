using System.Text;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FunctionsVsWebJobsPoc.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FunctionsVsWebJobsPoc.IntegrationTests;

/// <summary>
/// Small helpers shared by the parity tests: uploading identical inputs to both the
/// "function" and "webjob" resources, and polling the database until matching rows appear.
/// </summary>
internal static class TestDataHelper
{
    public static async Task UploadBlobToBothContainersAsync(string storageConnectionString, string blobName, string content)
    {
        var serviceClient = new BlobServiceClient(storageConnectionString);

        foreach (var containerName in new[] { "function", "webjob" })
        {
            var container = serviceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlobClient(blobName);
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            await blob.UploadAsync(stream, overwrite: true);
        }
    }

    public static async Task SendMessageToBothQueuesAsync(string serviceBusConnectionString, string messageBody)
    {
        await using var client = new ServiceBusClient(serviceBusConnectionString);

        foreach (var queueName in new[] { "function", "webjob" })
        {
            await using var sender = client.CreateSender(queueName);
            await sender.SendMessageAsync(new ServiceBusMessage(messageBody));
        }
    }

    public static async Task<T> PollUntilAsync<T>(Func<Task<T?>> probe, TimeSpan timeout, TimeSpan interval)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var result = await probe();
            if (result is not null)
            {
                return result;
            }

            await Task.Delay(interval);
        }

        throw new TimeoutException("Condition was not met within the allotted time.");
    }

    public static PocDbContext CreateDbContext(string sqlConnectionString)
    {
        var options = new DbContextOptionsBuilder<PocDbContext>()
            .UseSqlServer(sqlConnectionString)
            .Options;
        return new PocDbContext(options);
    }
}
