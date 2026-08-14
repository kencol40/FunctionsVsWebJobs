using FunctionsVsWebJobsPoc.Core.Processing;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;

namespace FunctionsVsWebJobsPoc.WebJobApp;

/// <summary>
/// Thin WebJobs SDK trigger entry point mirroring <c>ServiceBusTriggerFunction</c> in the Function App.
/// </summary>
public class ServiceBusJobFunctions
{
    private readonly IServiceBusMessageProcessor _messageProcessor;
    private readonly ILogger<ServiceBusJobFunctions> _logger;

    public ServiceBusJobFunctions(IServiceBusMessageProcessor messageProcessor, ILogger<ServiceBusJobFunctions> logger)
    {
        _messageProcessor = messageProcessor;
        _logger = logger;
    }

    public async Task ProcessMessage(
        [ServiceBusTrigger("webjob-queue", Connection = "ServiceBusConnection")] string messageBody,
        string messageId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("WebJob service bus trigger fired for message {MessageId}", messageId);
        await _messageProcessor.ProcessAsync(ProcessingTarget.WebJob, messageId, messageBody, cancellationToken);
    }
}
