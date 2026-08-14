using FunctionsVsWebJobsPoc.Core.Data;
using FunctionsVsWebJobsPoc.Core.Entities;
using FunctionsVsWebJobsPoc.Core.Processing;

namespace FunctionsVsWebJobsPoc.Core.Repositories;

public interface IBlobRowRepository
{
    Task AddRowsAsync(ProcessingTarget target, string blobName, IEnumerable<string> rowsJson, CancellationToken cancellationToken);
}

/// <summary>
/// Persists parsed CSV rows to the table matching the requested <see cref="ProcessingTarget"/>.
/// Kept isolated behind an interface so processors can be unit tested without a real database.
/// </summary>
public class BlobRowRepository : IBlobRowRepository
{
    private readonly PocDbContext _dbContext;

    public BlobRowRepository(PocDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRowsAsync(ProcessingTarget target, string blobName, IEnumerable<string> rowsJson, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        switch (target)
        {
            case ProcessingTarget.Function:
                foreach (var rowJson in rowsJson)
                {
                    _dbContext.FunctionBlobRows.Add(new FunctionBlobRow
                    {
                        BlobName = blobName,
                        RowJson = rowJson,
                        CreatedUtc = now
                    });
                }
                break;

            case ProcessingTarget.WebJob:
                foreach (var rowJson in rowsJson)
                {
                    _dbContext.WebJobBlobRows.Add(new WebJobBlobRow
                    {
                        BlobName = blobName,
                        RowJson = rowJson,
                        CreatedUtc = now
                    });
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, "Unsupported processing target.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
