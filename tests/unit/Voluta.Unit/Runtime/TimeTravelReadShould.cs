using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Runtime;

public sealed class TimeTravelReadShould
{
    [Fact(DisplayName = "Given unknown thread, when GetStateAsync, then returns null")]
    public async Task GetStateMissingThreadReturnsNull()
    {
        var graph = BuildLinear(new InMemoryCheckpointer());

        var state = await graph.GetStateAsync("missing-thread");

        state.ShouldBeNull();
    }

    [Fact(DisplayName = "Given empty thread id, when GetStateAsync, then throws ArgumentException")]
    public async Task GetStateEmptyThreadIdThrows()
    {
        var graph = BuildLinear(new InMemoryCheckpointer());

        await Should.ThrowAsync<ArgumentException>(
            async () => await graph.GetStateAsync("  "));
    }

    [Fact(DisplayName = "Given completed run, when GetStateAsync, then returns Done with channel values")]
    public async Task GetStateAfterDone()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = BuildLinear(checkpointer);

        await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "tt-done", StreamMode = StreamMode.Values });

        var state = await graph.GetStateAsync("tt-done");

        state.ShouldNotBeNull();
        state.ThreadId.ShouldBe("tt-done");
        state.Status.ShouldBe(GraphRunStatus.Done);
        state.Step.ShouldBeGreaterThan(0);
        var messages = state.Values["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldContain("a");
        messages.ShouldContain("b");
    }

    [Fact(DisplayName = "Given multi-step run, when GetHistoryAsync, then ordered by step and last matches GetState")]
    public async Task GetHistoryOrderedAndMatchesLatest()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = BuildLinear(checkpointer);

        await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "tt-hist", StreamMode = StreamMode.Values });

        var history = await graph.GetHistoryAsync("tt-hist");
        var latest = await graph.GetStateAsync("tt-hist");

        history.Count.ShouldBeGreaterThanOrEqualTo(1);
        for (var index = 1; index < history.Count; index++)
        {
            history[index].Step.ShouldBeGreaterThanOrEqualTo(history[index - 1].Step);
        }

        latest.ShouldNotBeNull();
        history[^1].Step.ShouldBe(latest.Step);
        history[^1].Status.ShouldBe(latest.Status);
        history[^1].ThreadId.ShouldBe(latest.ThreadId);
    }

    [Fact(DisplayName = "Given interrupt, when GetStateAsync, then Interrupted with interrupt payload")]
    public async Task GetStateInterrupted()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "gate",
                static (context, _) => context.ResumePayload is null
                    ? Task.FromResult<NodeResult>(NodeResult.Interrupt("need-approve"))
                    : Task.FromResult<NodeResult>(
                        NodeResult.Continue(new ChannelWrite("messages", "ok"))))
            .AddEdge(GraphConstants.Start, "gate")
            .AddEdge("gate", GraphConstants.End)
            .Compile(checkpointer);

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "tt-int", StreamMode = StreamMode.Events });

        terminal.Kind.ShouldBe(StreamEventKind.Interrupt);

        var state = await graph.GetStateAsync("tt-int");

        state.ShouldNotBeNull();
        state.Status.ShouldBe(GraphRunStatus.Interrupted);
        state.InterruptPayload.ShouldBe("need-approve");
    }

    [Fact(DisplayName = "Given unknown thread, when GetHistoryAsync, then empty list")]
    public async Task GetHistoryMissingEmpty()
    {
        var graph = BuildLinear(new InMemoryCheckpointer());

        var history = await graph.GetHistoryAsync("no-such");

        history.ShouldBeEmpty();
    }

    private static Voluta.Graph.CompiledGraph BuildLinear(InMemoryCheckpointer checkpointer)
    {
        return new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "a",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "a"))))
            .AddNode(
                "b",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "b"))))
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", "b")
            .AddEdge("b", GraphConstants.End)
            .Compile(checkpointer);
    }
}
