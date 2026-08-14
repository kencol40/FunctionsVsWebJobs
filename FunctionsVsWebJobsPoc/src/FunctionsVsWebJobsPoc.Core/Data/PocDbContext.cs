using FunctionsVsWebJobsPoc.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FunctionsVsWebJobsPoc.Core.Data;

/// <summary>
/// Injectable EF Core context shared by both the Function App and the WebJob host.
/// Table names are fixed so that both the Functions stack and the WebJobs stack write to
/// their own dedicated tables while sharing the exact same DbContext/entity definitions.
/// </summary>
public class PocDbContext : DbContext
{
    public PocDbContext(DbContextOptions<PocDbContext> options)
        : base(options)
    {
    }

    public DbSet<FunctionBlobRow> FunctionBlobRows => Set<FunctionBlobRow>();
    public DbSet<WebJobBlobRow> WebJobBlobRows => Set<WebJobBlobRow>();
    public DbSet<FunctionMessageData> FunctionMessageData => Set<FunctionMessageData>();
    public DbSet<WebJobMessageData> WebJobMessageData => Set<WebJobMessageData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FunctionBlobRow>(e =>
        {
            e.ToTable("function_blobrow_data");
            e.HasKey(x => x.Id);
            e.Property(x => x.BlobName).HasMaxLength(1024).IsRequired();
            e.Property(x => x.RowJson).IsRequired();
        });

        modelBuilder.Entity<WebJobBlobRow>(e =>
        {
            e.ToTable("webjob_blobrow_data");
            e.HasKey(x => x.Id);
            e.Property(x => x.BlobName).HasMaxLength(1024).IsRequired();
            e.Property(x => x.RowJson).IsRequired();
        });

        modelBuilder.Entity<FunctionMessageData>(e =>
        {
            e.ToTable("function_message_data");
            e.HasKey(x => x.Id);
            e.Property(x => x.MessageId).HasMaxLength(256).IsRequired();
            e.Property(x => x.BodyJson).IsRequired();
        });

        modelBuilder.Entity<WebJobMessageData>(e =>
        {
            e.ToTable("webjob_message_data");
            e.HasKey(x => x.Id);
            e.Property(x => x.MessageId).HasMaxLength(256).IsRequired();
            e.Property(x => x.BodyJson).IsRequired();
        });
    }
}
