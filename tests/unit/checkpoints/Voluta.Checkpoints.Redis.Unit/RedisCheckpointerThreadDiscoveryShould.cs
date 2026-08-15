using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Checkpoints.Redis;
using Xunit;

namespace Voluta.Checkpoints.Redis.Unit;

public sealed class RedisCheckpointerThreadDiscoveryShould
{
    [Fact(DisplayName = "Given threads in the store, when ListThreadIds, then sanitized ids return sorted")]
    public async Task ListThreadIdsReturnsSorted()
    {
        var checkpointer = CreateCheckpointer();

        await checkpointer.PutAsync(Snapshot("beta"));
        await checkpointer.PutAsync(Snapshot("alpha"));
        await checkpointer.PutAsync(Snapshot("gamma"));

        var ids = await checkpointer.ListThreadIdsAsync();

        ids.ShouldBe(["alpha", "beta", "gamma"]);
    }

    [Fact(DisplayName = "Given thread id with unsafe key chars, when Put + ListThreadIds, then id survives sanitized")]
    public async Task UnsafeThreadIdIsSanitizedInKeys()
    {
        var checkpointer = CreateCheckpointer();

        await checkpointer.PutAsync(Snapshot("tenant:with spaces"));
        await checkpointer.PutAsync(Snapshot("plain"));

        var ids = await checkpointer.ListThreadIdsAsync();

        ids.ShouldContain("tenant_with_spaces");
        ids.ShouldContain("plain");
    }

    [Fact(DisplayName = "Given empty store, when ListThreadIds, then empty list")]
    public async Task EmptyStoreReturnsEmpty()
    {
        var checkpointer = CreateCheckpointer();

        var ids = await checkpointer.ListThreadIdsAsync();

        ids.ShouldBeEmpty();
    }

    private static RedisCheckpointer CreateCheckpointer()
    {
        return new RedisCheckpointer(
            new InMemoryRedisCheckpointStore(),
            new RedisCheckpointerOptions());
    }

    private static CheckpointSnapshot Snapshot(string threadId)
    {
        return new CheckpointSnapshot
        {
            ThreadId = threadId,
            Step = 1,
            ChannelValues = new Dictionary<string, object?> { ["seed"] = "v" },
            NextNodes = [],
        };
    }
}
