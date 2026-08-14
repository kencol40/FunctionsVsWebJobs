using FunctionsVsWebJobsPoc.Core.Processing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionsVsWebJobsPoc.FunctionApp;

/// <summary>
/// Thin trigger entry point: all real work is delegated to <see cref="IServiceBusMessageProcessor"/>
/// so the handler logic can be unit tested independently of the Functions runtime.
/// </summary>
public class ServiceBusTriggerFunction
{
    private readonly IServiceBusMessageProcessor _messageProcessor;
    private readonly ILogger<ServiceBusTriggerFunction> _logger;

    public ServiceBusTriggerFunction(IServiceBusMessageProcessor messageProcessor, ILogger<ServiceBusTriggerFunction> logger)
    {
        _messageProcessor = messageProcessor;
        _logger = logger;
    }

    [Function(nameof(ServiceBusTriggerFunction))]
    public async Task Run(
        [ServiceBusTrigger("function-queue", Connection = "ServiceBusConnection")] string messageBody,
        string messageId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Function service bus trigger fired for message {MessageId}", messageId);
        await _messageProcessor.ProcessAsync(ProcessingTarget.Function, messageId, messageBody, cancellationToken);
    }
}
