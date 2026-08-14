using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;
using Voluta.DependencyInjection;
using Xunit;

namespace Voluta.Checkpoints.EntityFrameworkCore.Unit;

public sealed class EntityFrameworkCoreCheckpointerThreadDiscoveryShould
{
    [Fact(DisplayName = "Given put threads, when ListThreadIdsAsync, then returns distinct ids ordered")]
    public async Task DiscoverDistinctThreadIds()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<DiscoveryHostDbContext>(options => options.UseSqlite(connection));
        services.AddVolutaCheckpoints(
            static checkpoints => checkpoints.UseEntityFrameworkCore<DiscoveryHostDbContext>());

        await using var provider = services.BuildServiceProvider();
        await using (var setup = await provider
                         .GetRequiredService<IDbContextFactory<DiscoveryHostDbContext>>()
                         .CreateDbContextAsync())
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var checkpointer = provider.GetRequiredService<ICheckpointer>();
        await checkpointer.PutAsync(
            new CheckpointSnapshot
            {
                ThreadId = "ef-b",
                Step = 1,
                Status = GraphRunStatus.Running,
            });
        await checkpointer.PutAsync(
            new CheckpointSnapshot
            {
                ThreadId = "ef-a",
                Step = 1,
                Status = GraphRunStatus.Running,
            });
        await checkpointer.PutAsync(
            new CheckpointSnapshot
            {
                ThreadId = "ef-a",
                Step = 2,
                Status = GraphRunStatus.Done,
            });

        var discovery = checkpointer.ShouldBeAssignableTo<IThreadDiscovery>();
        var ids = await discovery.ListThreadIdsAsync();

        ids.ShouldBe(["ef-a", "ef-b"]);
    }
}

file sealed class DiscoveryHostDbContext(DbContextOptions<DiscoveryHostDbContext> options)
    : DbContext(options), IVolutaCheckpointDbContext
{
    public DbSet<CheckpointRecord> Checkpoints => Set<CheckpointRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyVolutaCheckpointModel();
        base.OnModelCreating(modelBuilder);
    }
}
