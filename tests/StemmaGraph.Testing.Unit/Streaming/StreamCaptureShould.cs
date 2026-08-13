// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using Shouldly;
using StemmaGraph;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph;
using StemmaGraph.Runtime;
using StemmaGraph.Streaming;
using StemmaGraph.Testing.Fixtures;
using StemmaGraph.Testing.Streaming;
using Xunit;

namespace StemmaGraph.Testing.Unit.Streaming;

public sealed class StreamCaptureShould
{
    [Fact(DisplayName = "Given linear graph stream, when CollectAsync is called, then captures end event")]
    public async Task CollectLinearStreamEvents()
    {
        var graph = GraphFixtures.Linear().Compile(new InMemoryCheckpointer());
        var stream = graph.StreamAsync(
            [],
            new RunOptions { ThreadId = "capture-1", StreamMode = StreamMode.Events });

        var capture = await StreamCapture.CollectAsync(stream);

        capture.Events.ShouldNotBeEmpty();
        capture.Events.ShouldContain(streamEvent => streamEvent.Kind == StreamEventKind.End);
    }
}
