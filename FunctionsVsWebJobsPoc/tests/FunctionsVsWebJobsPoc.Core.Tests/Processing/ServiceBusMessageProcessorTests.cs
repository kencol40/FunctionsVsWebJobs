using FunctionsVsWebJobsPoc.Core.Processing;
using FunctionsVsWebJobsPoc.Core.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FunctionsVsWebJobsPoc.Core.Tests.Processing;

public class ServiceBusMessageProcessorTests
{
    [Fact]
    public async Task ProcessAsync_JsonBody_PassesThroughUnchanged()
    {
        var repositoryMock = new Mock<IMessageDataRepository>();
        var processor = new ServiceBusMessageProcessor(repositoryMock.Object, NullLogger<ServiceBusMessageProcessor>.Instance);

        await processor.ProcessAsync(ProcessingTarget.Function, "msg-1", "{\"orderId\":123}", CancellationToken.None);

        repositoryMock.Verify(r => r.AddMessageAsync(ProcessingTarget.Function, "msg-1", "{\"orderId\":123}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_PlainTextBody_WrapsInJsonObject()
    {
        var repositoryMock = new Mock<IMessageDataRepository>();
        var processor = new ServiceBusMessageProcessor(repositoryMock.Object, NullLogger<ServiceBusMessageProcessor>.Instance);

        await processor.ProcessAsync(ProcessingTarget.WebJob, "msg-2", "hello world", CancellationToken.None);

        repositoryMock.Verify(r => r.AddMessageAsync(
            ProcessingTarget.WebJob,
            "msg-2",
            It.Is<string>(json => json.Contains("hello world")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
