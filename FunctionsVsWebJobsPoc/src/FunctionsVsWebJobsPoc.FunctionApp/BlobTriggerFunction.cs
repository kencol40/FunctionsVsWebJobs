using FunctionsVsWebJobsPoc.Core.Processing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionsVsWebJobsPoc.FunctionApp;

/// <summary>
/// Thin trigger entry point: all real work is delegated to <see cref="IBlobRowProcessor"/>
/// so the handler logic can be unit tested independently of the Functions runtime.
/// </summary>
public class BlobTriggerFunction
{
    private readonly IBlobRowProcessor _blobRowProcessor;
    private readonly ILogger<BlobTriggerFunction> _logger;

    public BlobTriggerFunction(IBlobRowProcessor blobRowProcessor, ILogger<BlobTriggerFunction> logger)
    {
        _blobRowProcessor = blobRowProcessor;
        _logger = logger;
    }

    [Function(nameof(BlobTriggerFunction))]
    public async Task Run(
        [BlobTrigger("function/{name}", Connection = "AzureStorage")] Stream blobStream,
        string name,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Function blob trigger fired for {BlobName}", name);
        await _blobRowProcessor.ProcessAsync(ProcessingTarget.Function, name, blobStream, cancellationToken);
    }
}
