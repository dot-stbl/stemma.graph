using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;
using Voluta.DependencyInjection;
using Xunit;

namespace Voluta.Checkpoints.Postgres.Unit;

[Collection(nameof(PostgresCollection))]
public sealed class PostgresCheckpointerThreadDiscoveryShould(PostgresFixture fixture)
{
    [Fact(DisplayName = "Given two threads put, when ListThreadIdsAsync, then both ids are returned")]
    public async Task ListThreadIdsAfterPut()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        var checkpointer = fixture.CreateCheckpointer();
        var threadA = "discover-a-" + Guid.NewGuid().ToString("N");
        var threadB = "discover-b-" + Guid.NewGuid().ToString("N");

        await checkpointer.PutAsync(CreateSnapshot(threadA, step: 1));
        await checkpointer.PutAsync(CreateSnapshot(threadB, step: 1));

        checkpointer.ShouldBeAssignableTo<IThreadDiscovery>();
        var discovery = (IThreadDiscovery)checkpointer;
        var ids = await discovery.ListThreadIdsAsync();

        ids.ShouldContain(threadA);
        ids.ShouldContain(threadB);
    }

    [Fact(DisplayName = "Given UsePostgres registration, when ICheckpointer resolves, then type is PostgresCheckpointer")]
    public void UsePostgresRegistersCheckpointer()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        var connectionString = fixture.ConnectionString!;
        var services = new ServiceCollection();
        services.AddVolutaCheckpoints(checkpoints => checkpoints.UsePostgres(options =>
        {
            options.ConnectionString = connectionString;
        }));
        using var provider = services.BuildServiceProvider();

        var checkpointer = provider.GetRequiredService<ICheckpointer>();

        checkpointer.ShouldBeOfType<PostgresCheckpointer>();
        checkpointer.ShouldBeAssignableTo<IThreadDiscovery>();
    }

    private static CheckpointSnapshot CreateSnapshot(string threadId, long step)
    {
        return new CheckpointSnapshot
        {
            ThreadId = threadId,
            Step = step,
            Status = GraphRunStatus.Running,
        };
    }
}
