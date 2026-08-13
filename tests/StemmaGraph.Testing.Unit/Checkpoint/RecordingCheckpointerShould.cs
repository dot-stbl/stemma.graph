// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using Shouldly;
using StemmaGraph.Checkpoint;
using StemmaGraph.Runtime;
using Xunit;

namespace StemmaGraph.Testing.Unit.Checkpoint;

public sealed class RecordingCheckpointerShould
{
    [Fact(DisplayName = "Given Put then Get, when recording, then records both calls with payloads")]
    public async Task RecordPutAndGet()
    {
        var recording = new RecordingCheckpointer(new InMemoryCheckpointer());
        var snapshot = new CheckpointSnapshot
        {
            ThreadId = "rec-1",
            Step = 2,
            Status = GraphRunStatus.Interrupted,
            InterruptPayload = new { amount = 10 },
        };

        await recording.PutAsync(snapshot);
        var loaded = await recording.GetAsync("rec-1");

        recording.Puts.Count.ShouldBe(1);
        recording.Puts[0].ThreadId.ShouldBe("rec-1");
        recording.Puts[0].Status.ShouldBe(GraphRunStatus.Interrupted);
        recording.Gets.Count.ShouldBe(1);
        recording.Gets[0].ThreadId.ShouldBe("rec-1");
        recording.Gets[0].Result.ShouldNotBeNull();
        loaded!.Step.ShouldBe(2);
    }

    [Fact(DisplayName = "Given List after Puts, when recording, then records List with results")]
    public async Task RecordList()
    {
        var recording = new RecordingCheckpointer(new InMemoryCheckpointer());
        await recording.PutAsync(new CheckpointSnapshot { ThreadId = "rec-list", Step = 1, Status = GraphRunStatus.Running });
        await recording.PutAsync(new CheckpointSnapshot { ThreadId = "rec-list", Step = 2, Status = GraphRunStatus.Done });

        var list = await recording.ListAsync("rec-list");

        list.Count.ShouldBe(2);
        recording.Lists.Count.ShouldBe(1);
        recording.Lists[0].ThreadId.ShouldBe("rec-list");
        recording.Lists[0].Result.Count.ShouldBe(2);
    }
}
