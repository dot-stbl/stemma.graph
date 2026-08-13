using Shouldly;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Exceptions;
using StemmaGraph.Abstractions.Streaming;
using StemmaGraph.Testing.Fixtures;
using Xunit;
using StemmaGraph.Graph.Options;
using StemmaGraph.Exceptions.Run;

namespace StemmaGraph.Testing.Unit.Fixtures;

public sealed class GraphFixturesShould
{
    [Fact(DisplayName = "Given Linear fixture, when InvokeAsync is called, then reaches End")]
    public async Task LinearRunsToEnd()
    {
        var graph = GraphFixtures.Linear().Compile(new InMemoryCheckpointer());

        var terminal = await graph.InvokeAsync(
            [new ChannelWrite("messages", "seed")],
            new RunOptions { ThreadId = "fix-linear", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        var messages = terminal.State!["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldContain("seed");
        messages.ShouldContain("from-a");
    }

    [Fact(DisplayName = "Given Cycle fixture with low limit, when InvokeAsync is called, then fails out of steps")]
    public async Task CycleHitsRecursionLimit()
    {
        var graph = GraphFixtures.Cycle().Compile(
            new InMemoryCheckpointer(),
            new CompileOptions { RecursionLimit = 3 });

        await Should.ThrowAsync<GraphOutOfStepsException>(async () =>
        {
            await graph.InvokeAsync([], new RunOptions { ThreadId = "fix-cycle" });
        });
    }

    [Fact(DisplayName = "Given Interrupt fixture, when Invoke then Resume, then continues to End")]
    public async Task InterruptThenResume()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = GraphFixtures.Interrupt().Compile(checkpointer);

        var interrupted = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "fix-hitl", StreamMode = StreamMode.Events });

        interrupted.Kind.ShouldBe(StreamEventKind.Interrupt);

        var terminal = await graph.ResumeInvokeAsync(
            "fix-hitl",
            new Command { Kind = "approve", Payload = "ok" });

        terminal.Kind.ShouldBe(StreamEventKind.End);
    }

    [Fact(DisplayName = "Given MultiReady fixture, when InvokeAsync is called, then both nodes write in one superstep")]
    public async Task MultiReadyMergesAppend()
    {
        var graph = GraphFixtures.MultiReady().Compile(new InMemoryCheckpointer());

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "fix-multi", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        var messages = terminal.State!["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldBe(["L", "R"]);
    }
}
