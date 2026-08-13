// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using Shouldly;
using StemmaGraph.Checkpoint;
using StemmaGraph.Abstractions.Runtime;
using Xunit;
using StemmaGraph.Abstractions.Checkpoint;
using StemmaGraph.Testing.Checkpoint;

namespace StemmaGraph.Testing.Unit.Checkpoint;

public sealed class FaultInjectingCheckpointerShould
{
    [Fact(DisplayName = "Given fail on second Put, when two Puts, then first succeeds and second throws")]
    public async Task FailOnSecondPut()
    {
        var inner = new InMemoryCheckpointer();
        var fault = new FaultInjectingCheckpointer(inner, failOnPutNumber: 2);

        await fault.PutAsync(new CheckpointSnapshot { ThreadId = "f-1", Step = 1, Status = GraphRunStatus.Running });

        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await fault.PutAsync(new CheckpointSnapshot { ThreadId = "f-1", Step = 2, Status = GraphRunStatus.Running });
        });

        exception.Message.ShouldContain("Put #2");
        fault.PutAttempts.ShouldBe(2);

        var loaded = await inner.GetAsync("f-1");
        loaded!.Step.ShouldBe(1);
    }

    [Fact(DisplayName = "Given custom fault, when failing Put, then throws the configured exception")]
    public async Task UseCustomFault()
    {
        var custom = new IOException("disk full");
        var fault = new FaultInjectingCheckpointer(new InMemoryCheckpointer(), failOnPutNumber: 1, fault: custom);

        var thrown = await Should.ThrowAsync<IOException>(async () =>
        {
            await fault.PutAsync(new CheckpointSnapshot { ThreadId = "f-2", Step = 1, Status = GraphRunStatus.Running });
        });

        thrown.ShouldBeSameAs(custom);
    }

    [Fact(DisplayName = "Given failOnPutNumber less than 1, when constructed, then throws")]
    public void RejectInvalidFailOnPutNumber()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            _ = new FaultInjectingCheckpointer(new InMemoryCheckpointer(), failOnPutNumber: 0);
        });
    }
}
