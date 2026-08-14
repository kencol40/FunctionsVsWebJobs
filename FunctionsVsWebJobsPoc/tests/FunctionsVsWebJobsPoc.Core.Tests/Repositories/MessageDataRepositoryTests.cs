using FunctionsVsWebJobsPoc.Core.Data;
using FunctionsVsWebJobsPoc.Core.Processing;
using FunctionsVsWebJobsPoc.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FunctionsVsWebJobsPoc.Core.Tests.Repositories;

public class MessageDataRepositoryTests
{
    private static PocDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<PocDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new PocDbContext(options);
    }

    [Fact]
    public async Task AddMessageAsync_Function_WritesToFunctionMessageData()
    {
        await using var context = CreateContext(nameof(AddMessageAsync_Function_WritesToFunctionMessageData));
        var repository = new MessageDataRepository(context);

        await repository.AddMessageAsync(ProcessingTarget.Function, "msg-1", "{\"body\":\"x\"}", CancellationToken.None);

        Assert.Single(context.FunctionMessageData);
        Assert.Empty(context.WebJobMessageData);
    }

    [Fact]
    public async Task AddMessageAsync_WebJob_WritesToWebJobMessageData()
    {
        await using var context = CreateContext(nameof(AddMessageAsync_WebJob_WritesToWebJobMessageData));
        var repository = new MessageDataRepository(context);

        await repository.AddMessageAsync(ProcessingTarget.WebJob, "msg-2", "{\"body\":\"y\"}", CancellationToken.None);

        Assert.Single(context.WebJobMessageData);
        Assert.Empty(context.FunctionMessageData);
    }
}
