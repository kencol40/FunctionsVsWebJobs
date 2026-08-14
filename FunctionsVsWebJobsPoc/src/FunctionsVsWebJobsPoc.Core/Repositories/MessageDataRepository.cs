using FunctionsVsWebJobsPoc.Core.Data;
using FunctionsVsWebJobsPoc.Core.Entities;
using FunctionsVsWebJobsPoc.Core.Processing;

namespace FunctionsVsWebJobsPoc.Core.Repositories;

public interface IMessageDataRepository
{
    Task AddMessageAsync(ProcessingTarget target, string messageId, string bodyJson, CancellationToken cancellationToken);
}

/// <summary>
/// Persists a service bus message body to the table matching the requested <see cref="ProcessingTarget"/>.
/// </summary>
public class MessageDataRepository : IMessageDataRepository
{
    private readonly PocDbContext _dbContext;

    public MessageDataRepository(PocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddMessageAsync(ProcessingTarget target, string messageId, string bodyJson, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        switch (target)
        {
            case ProcessingTarget.Function:
                _dbContext.FunctionMessageData.Add(new FunctionMessageData
                {
                    MessageId = messageId,
                    BodyJson = bodyJson,
                    CreatedUtc = now
                });
                break;

            case ProcessingTarget.WebJob:
                _dbContext.WebJobMessageData.Add(new WebJobMessageData
                {
                    MessageId = messageId,
                    BodyJson = bodyJson,
                    CreatedUtc = now
                });
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, "Unsupported processing target.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
