using FunctionsVsWebJobsPoc.Core.Csv;
using FunctionsVsWebJobsPoc.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace FunctionsVsWebJobsPoc.Core.Processing;

public interface IBlobRowProcessor
{
    Task ProcessAsync(ProcessingTarget target, string blobName, Stream csvStream, CancellationToken cancellationToken);
}

/// <summary>
/// Shared handler used by both the Function blob trigger and the WebJob blob trigger.
/// Keeps the actual trigger entry points thin and testable in isolation via mocks.
/// </summary>
public class BlobRowProcessor : IBlobRowProcessor
{
    private readonly ICsvRowParser _csvRowParser;
    private readonly IBlobRowRepository _blobRowRepository;
    private readonly ILogger<BlobRowProcessor> _logger;

    public BlobRowProcessor(ICsvRowParser csvRowParser, IBlobRowRepository blobRowRepository, ILogger<BlobRowProcessor> logger)
    {
        _csvRowParser = csvRowParser;
        _blobRowRepository = blobRowRepository;
        _logger = logger;
    }

    public async Task ProcessAsync(ProcessingTarget target, string blobName, Stream csvStream, CancellationToken cancellationToken)
    {
        var rows = _csvRowParser.ParseRowsAsJson(csvStream);

        _logger.LogInformation("Parsed {RowCount} rows from blob {BlobName} for target {Target}", rows.Count, blobName, target);

        if (rows.Count == 0)
        {
            return;
        }

        await _blobRowRepository.AddRowsAsync(target, blobName, rows, cancellationToken);
    }
}
