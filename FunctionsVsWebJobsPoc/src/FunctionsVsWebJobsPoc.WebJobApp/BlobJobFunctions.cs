using FunctionsVsWebJobsPoc.Core.Processing;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace FunctionsVsWebJobsPoc.WebJobApp;

/// <summary>
/// Thin WebJobs SDK trigger entry point mirroring <c>BlobTriggerFunction</c> in the Function App.
/// Delegates all real work to the shared <see cref="IBlobRowProcessor"/> so the same handler
/// logic is unit-testable independently of the WebJobs host.
/// </summary>
public class BlobJobFunctions
{
    private readonly IBlobRowProcessor _blobRowProcessor;
    private readonly ILogger<BlobJobFunctions> _logger;

    public BlobJobFunctions(IBlobRowProcessor blobRowProcessor, ILogger<BlobJobFunctions> logger)
    {
        _blobRowProcessor = blobRowProcessor;
        _logger = logger;
    }

    public async Task ProcessBlob(
        [BlobTrigger("webjob/{name}", Connection = "AzureStorage")] Stream blobStream,
        string name,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("WebJob blob trigger fired for {BlobName}", name);
        await _blobRowProcessor.ProcessAsync(ProcessingTarget.WebJob, name, blobStream, cancellationToken);
    }
}
