using FunctionsVsWebJobsPoc.Core.Data;
using FunctionsVsWebJobsPoc.Core.Processing;
using FunctionsVsWebJobsPoc.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FunctionsVsWebJobsPoc.Core.Tests.Repositories;

public class BlobRowRepositoryTests
{
    private static PocDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<PocDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new PocDbContext(options);
    }

    [Fact]
    public async Task AddRowsAsync_Function_WritesToFunctionBlobRows()
    {
        await using var context = CreateContext(nameof(AddRowsAsync_Function_WritesToFunctionBlobRows));
        var repository = new BlobRowRepository(context);

        await repository.AddRowsAsync(ProcessingTarget.Function, "orders.csv", new[] { "{\"a\":1}", "{\"a\":2}" }, CancellationToken.None);

        Assert.Equal(2, context.FunctionBlobRows.Count());
        Assert.Empty(context.WebJobBlobRows);
    }

    [Fact]
    public async Task AddRowsAsync_WebJob_WritesToWebJobBlobRows()
    {
        await using var context = CreateContext(nameof(AddRowsAsync_WebJob_WritesToWebJobBlobRows));
        var repository = new BlobRowRepository(context);

        await repository.AddRowsAsync(ProcessingTarget.WebJob, "orders.csv", new[] { "{\"a\":1}" }, CancellationToken.None);

        Assert.Single(context.WebJobBlobRows);
        Assert.Empty(context.FunctionBlobRows);
    }
}
