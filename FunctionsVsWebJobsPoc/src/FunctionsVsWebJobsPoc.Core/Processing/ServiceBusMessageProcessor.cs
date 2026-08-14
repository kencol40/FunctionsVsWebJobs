using System.Text.Json;
using FunctionsVsWebJobsPoc.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace FunctionsVsWebJobsPoc.Core.Processing;

public interface IServiceBusMessageProcessor
{
    Task ProcessAsync(ProcessingTarget target, string messageId, string messageBody, CancellationToken cancellationToken);
}

/// <summary>
/// Shared handler used by both the Function service bus trigger and the WebJob service bus trigger.
/// </summary>
public class ServiceBusMessageProcessor : IServiceBusMessageProcessor
{
    private readonly IMessageDataRepository _messageDataRepository;
    private readonly ILogger<ServiceBusMessageProcessor> _logger;

    public ServiceBusMessageProcessor(IMessageDataRepository messageDataRepository, ILogger<ServiceBusMessageProcessor> logger)
    {
        _messageDataRepository = messageDataRepository;
        _logger = logger;
    }

    public async Task ProcessAsync(ProcessingTarget target, string messageId, string messageBody, CancellationToken cancellationToken)
    {
        var bodyJson = NormalizeToJson(messageBody);

        _logger.LogInformation("Processing message {MessageId} for target {Target}", messageId, target);

        await _messageDataRepository.AddMessageAsync(target, messageId, bodyJson, cancellationToken);
    }

    private static string NormalizeToJson(string messageBody)
    {
        if (string.IsNullOrWhiteSpace(messageBody))
        {
            return JsonSerializer.Serialize(new { body = string.Empty });
        }

        try
        {
            using var document = JsonDocument.Parse(messageBody);
            return document.RootElement.GetRawText();
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { body = messageBody });
        }
    }
}
