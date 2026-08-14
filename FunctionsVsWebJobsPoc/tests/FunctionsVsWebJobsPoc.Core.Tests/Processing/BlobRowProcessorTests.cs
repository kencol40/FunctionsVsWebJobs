using System.Text;
using FunctionsVsWebJobsPoc.Core.Csv;
using FunctionsVsWebJobsPoc.Core.Processing;
using FunctionsVsWebJobsPoc.Core.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FunctionsVsWebJobsPoc.Core.Tests.Processing;

public class BlobRowProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ParsesCsvAndAddsRowsToRepository()
    {
        var csvParserMock = new Mock<ICsvRowParser>();
        var expectedRows = new List<string> { "{\"Name\":\"Alice\"}", "{\"Name\":\"Bob\"}" };
        csvParserMock.Setup(p => p.ParseRowsAsJson(It.IsAny<Stream>())).Returns(expectedRows);

        var repositoryMock = new Mock<IBlobRowRepository>();

        var processor = new BlobRowProcessor(csvParserMock.Object, repositoryMock.Object, NullLogger<BlobRowProcessor>.Instance);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name\nAlice\nBob\n"));

        await processor.ProcessAsync(ProcessingTarget.Function, "test.csv", stream, CancellationToken.None);

        repositoryMock.Verify(r => r.AddRowsAsync(ProcessingTarget.Function, "test.csv", expectedRows, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_NoRowsParsed_DoesNotCallRepository()
    {
        var csvParserMock = new Mock<ICsvRowParser>();
        csvParserMock.Setup(p => p.ParseRowsAsJson(It.IsAny<Stream>())).Returns(new List<string>());

        var repositoryMock = new Mock<IBlobRowRepository>();

        var processor = new BlobRowProcessor(csvParserMock.Object, repositoryMock.Object, NullLogger<BlobRowProcessor>.Instance);

        using var stream = new MemoryStream();

        await processor.ProcessAsync(ProcessingTarget.WebJob, "empty.csv", stream, CancellationToken.None);

        repositoryMock.Verify(r => r.AddRowsAsync(It.IsAny<ProcessingTarget>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
