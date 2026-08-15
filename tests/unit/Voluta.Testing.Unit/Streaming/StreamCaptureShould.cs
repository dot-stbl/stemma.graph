using Shouldly;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Testing.Fixtures;
using Voluta.Testing.Streaming;
using Xunit;

namespace Voluta.Testing.Unit.Streaming;

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
        capture.Events.ShouldContain(static streamEvent => streamEvent.Kind == StreamEventKind.End);
    }
}
