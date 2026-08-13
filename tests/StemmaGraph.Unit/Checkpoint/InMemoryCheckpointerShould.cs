using Shouldly;
using StemmaGraph.Abstractions.Checkpoint;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Checkpoint;
using Xunit;

namespace StemmaGraph.Unit.Checkpoint;

public sealed class InMemoryCheckpointerShould
{
    [Fact(DisplayName = "Given unknown thread, when GetAsync is called, then returns null")]
    public async Task ReturnNullOnGetMiss()
    {
        var checkpointer = new InMemoryCheckpointer();

        var snapshot = await checkpointer.GetAsync("missing-thread");

        snapshot.ShouldBeNull();
    }

    [Fact(DisplayName = "Given put snapshot, when GetAsync is called, then roundtrips C-shape fields")]
    public async Task RoundtripPutGet()
    {
        var checkpointer = new InMemoryCheckpointer();
        var original = new CheckpointSnapshot
        {
            ThreadId = "t1",
            Step = 3,
            Status = GraphRunStatus.Running,
            ChannelValues = new Dictionary<string, object?> { ["messages"] = new List<object?> { "a" } },
            ChannelVersions = new Dictionary<string, long> { ["messages"] = 2 },
            VersionsSeen = new Dictionary<string, IReadOnlyDictionary<string, long>>
            {
                ["agent"] = new Dictionary<string, long> { ["messages"] = 1 },
            },
            PendingWrites =
            [
                new PendingWrite { TaskId = "agent", ChannelName = "messages", Value = "x" },
            ],
            LastNode = "agent",
            NextNodes = ["tools"],
            InterruptPayload = null,
        };

        await checkpointer.PutAsync(original);
        var loaded = await checkpointer.GetAsync("t1");

        _ = loaded.ShouldNotBeNull();
        loaded!.Step.ShouldBe(3);
        loaded.Status.ShouldBe(GraphRunStatus.Running);
        loaded.LastNode.ShouldBe("agent");
        loaded.NextNodes.ShouldBe(["tools"]);
        loaded.ChannelVersions["messages"].ShouldBe(2);
        loaded.VersionsSeen["agent"]["messages"].ShouldBe(1);
        loaded.PendingWrites.Count.ShouldBe(1);
        loaded.PendingWrites[0].TaskId.ShouldBe("agent");
    }

    [Fact(DisplayName = "Given multiple steps, when ListAsync is called, then returns ordered by step")]
    public async Task ListOrderedByStep()
    {
        var checkpointer = new InMemoryCheckpointer();
        await checkpointer.PutAsync(new CheckpointSnapshot { ThreadId = "t", Step = 1, Status = GraphRunStatus.Running });
        await checkpointer.PutAsync(new CheckpointSnapshot { ThreadId = "t", Step = 2, Status = GraphRunStatus.Done });

        var list = await checkpointer.ListAsync("t");

        list.Count.ShouldBe(2);
        list[0].Step.ShouldBe(1);
        list[1].Step.ShouldBe(2);
    }
}
