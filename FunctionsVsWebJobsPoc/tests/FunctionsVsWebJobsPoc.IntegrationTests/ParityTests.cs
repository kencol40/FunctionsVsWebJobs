using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using FunctionsVsWebJobsPoc.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FunctionsVsWebJobsPoc.IntegrationTests;

/// <summary>
/// End-to-end parity tests: the same blob upload / message send is applied to both the
/// "function" and "webjob" resources, and the resulting rows in the respective SQL tables
/// are asserted to match. Requires Docker Desktop to be running (Aspire spins up Azurite,
/// the Service Bus emulator and a SQL Server container).
/// </summary>
public class ParityTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private string _storageConnectionString = string.Empty;
    private string _serviceBusConnectionString = string.Empty;
    private string _sqlConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.FunctionsVsWebJobsPoc_AppHost>();
        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        await _app.ResourceNotifications.WaitForResourceHealthyAsync("jebjobPoc");
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("storage");
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("servicebus");
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("functionapp");
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("webjobapp");

        _storageConnectionString = await _app.GetConnectionStringAsync("AzureStorage") ?? throw new InvalidOperationException("Storage connection string not available.");
        _serviceBusConnectionString = await _app.GetConnectionStringAsync("servicebus") ?? throw new InvalidOperationException("Service Bus connection string not available.");
        _sqlConnectionString = await _app.GetConnectionStringAsync("sqldb") ?? throw new InvalidOperationException("SQL connection string not available.");
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task SameCsvBlob_ProducesMatchingRows_InFunctionAndWebJobTables()
    {
        var blobName = $"parity-{Guid.NewGuid():N}.csv";
        const string csvContent = "Name,Age\nAlice,30\nBob,25\n";

        await TestDataHelper.UploadBlobToBothContainersAsync(_storageConnectionString, blobName, csvContent);

        var result = await TestDataHelper.PollUntilAsync<(List<string> Function, List<string> WebJob)?>(async () =>
        {
            await using var db = TestDataHelper.CreateDbContext(_sqlConnectionString);

            var fnRows = db.FunctionBlobRows.Where(r => r.BlobName == blobName).OrderBy(r => r.Id).Select(r => r.RowJson).ToList();
            var wjRows = db.WebJobBlobRows.Where(r => r.BlobName == blobName).OrderBy(r => r.Id).Select(r => r.RowJson).ToList();

            return fnRows.Count == 2 && wjRows.Count == 2 ? (fnRows, wjRows) : null;
        }, TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(2));

        Assert.Equal(result!.Value.Function, result.Value.WebJob);
    }

    [Fact]
    public async Task SameServiceBusMessage_ProducesMatchingRows_InFunctionAndWebJobTables()
    {
        var messageId = $"parity-{Guid.NewGuid():N}";
        var messageBody = $"{{\"messageId\":\"{messageId}\",\"payload\":\"hello\"}}";

        await TestDataHelper.SendMessageToBothQueuesAsync(_serviceBusConnectionString, messageBody);

        var result = await TestDataHelper.PollUntilAsync<(string Function, string WebJob)?>(async () =>
        {
            await using var db = TestDataHelper.CreateDbContext(_sqlConnectionString);

            var fn = db.FunctionMessageData.FirstOrDefault(r => r.BodyJson.Contains(messageId));
            var wj = db.WebJobMessageData.FirstOrDefault(r => r.BodyJson.Contains(messageId));

            return fn is not null && wj is not null ? (fn.BodyJson, wj.BodyJson) : null;
        }, TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(2));

        Assert.Equal(result!.Value.Function, result.Value.WebJob);
    }
}
