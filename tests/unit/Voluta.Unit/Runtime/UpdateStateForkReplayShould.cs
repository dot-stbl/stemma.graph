using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Diagnostics;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Exceptions.Run;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Runtime;

public sealed class UpdateStateForkReplayShould
{
    [Fact(DisplayName = "Given missing thread, when UpdateStateAsync, then GraphThreadNotFoundException")]
    public async Task UpdateStateMissingThreadThrows()
    {
        var graph = BuildLinear(new InMemoryCheckpointer());

        var exception = await Should.ThrowAsync<GraphThreadNotFoundException>(
            async () => await graph.UpdateStateAsync(
                "no-thread",
                [new ChannelWrite("messages", "x")]));

        exception.Code.ShouldBe(VolutaErrorCodes.GraphThreadNotFound);
    }

    [Fact(DisplayName = "Given interrupt, when UpdateStateAsync then Resume, then channel patch is visible")]
    public async Task UpdateStateThenResumeInterrupted()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "gate",
                static (context, _) => context.ResumePayload is null
                    ? Task.FromResult<NodeResult>(NodeResult.Interrupt("need-approve"))
                    : Task.FromResult<NodeResult>(
                        NodeResult.Continue(new ChannelWrite("messages", "after-resume"))))
            .AddEdge(GraphConstants.Start, "gate")
            .AddEdge("gate", GraphConstants.End)
            .Compile(checkpointer);

        await graph.InvokeAsync(
            [new ChannelWrite("messages", "seed")],
            new RunOptions { ThreadId = "upd-int", StreamMode = StreamMode.Events });

        var updated = await graph.UpdateStateAsync(
            "upd-int",
            [new ChannelWrite("messages", "host-patch")]);

        updated.Status.ShouldBe(GraphRunStatus.Interrupted);
        var values = updated.Values["messages"].ShouldBeOfType<List<object?>>();
        values.ShouldContain("seed");
        values.ShouldContain("host-patch");

        var terminal = await graph.ResumeInvokeAsync("upd-int", Command.Approve("ok"));

        terminal.Kind.ShouldBe(StreamEventKind.End);
        var done = await graph.GetStateAsync("upd-int");
        done.ShouldNotBeNull();
        done.Status.ShouldBe(GraphRunStatus.Done);
        var final = done.Values["messages"].ShouldBeOfType<List<object?>>();
        final.ShouldContain("host-patch");
        final.ShouldContain("after-resume");
    }

    [Fact(DisplayName = "Given mid-run fork at step, when Continue on new thread, then independent of source")]
    public async Task ForkThenIndependentContinue()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = BuildLinear(checkpointer);

        await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "src-fork", StreamMode = StreamMode.Values });

        var history = await graph.GetHistoryAsync("src-fork");
        history.Count.ShouldBeGreaterThanOrEqualTo(2);

        // Fork a mid-history Running step when present; else first non-terminal-looking step.
        var forkStep = history.FirstOrDefault(static item => item.Status == GraphRunStatus.Running)?.Step
                       ?? history[0].Step;

        var forked = await graph.ForkAsync("src-fork", forkStep, "fork-child");

        forked.ThreadId.ShouldBe("fork-child");
        forked.Step.ShouldBe(forkStep);

        var sourceLatest = await graph.GetStateAsync("src-fork");
        sourceLatest.ShouldNotBeNull();
        sourceLatest.Status.ShouldBe(GraphRunStatus.Done);

        var childState = await graph.GetStateAsync("fork-child");
        childState.ShouldNotBeNull();
        childState.ThreadId.ShouldBe("fork-child");
        childState.Step.ShouldBe(forkStep);

        // Patch child only — source history must not gain the write.
        await graph.UpdateStateAsync(
            "fork-child",
            [new ChannelWrite("messages", "only-child")]);

        var sourceHistory = await graph.GetHistoryAsync("src-fork");
        foreach (var step in sourceHistory)
        {
            if (step.Values.TryGetValue("messages", out var raw) && raw is List<object?> list)
            {
                list.ShouldNotContain("only-child");
            }
        }

        var childAfter = await graph.GetStateAsync("fork-child");
        childAfter.ShouldNotBeNull();
        var childMessages = childAfter.Values["messages"].ShouldBeOfType<List<object?>>();
        childMessages.ShouldContain("only-child");
    }

    [Fact(DisplayName = "Given missing step, when ForkAsync, then GraphStepNotFoundException")]
    public async Task ForkMissingStepThrows()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = BuildLinear(checkpointer);

        await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "fork-miss", StreamMode = StreamMode.Values });

        var exception = await Should.ThrowAsync<GraphStepNotFoundException>(
            async () => await graph.ForkAsync("fork-miss", 999_999, "fork-x"));

        exception.Code.ShouldBe(VolutaErrorCodes.GraphStepNotFound);
    }

    [Fact(DisplayName = "Given Done thread, when ContinueAsync, then GraphInvalidContinueException")]
    public async Task ContinueDoneThrows()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = BuildLinear(checkpointer);

        await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "cont-done", StreamMode = StreamMode.Values });

        var exception = await Should.ThrowAsync<GraphInvalidContinueException>(
            async () => await graph.ContinueInvokeAsync("cont-done"));

        exception.Code.ShouldBe(VolutaErrorCodes.GraphInvalidContinue);
    }

    [Fact(DisplayName = "Given interrupt mid-graph, when UpdateState + Continue path docs: use Resume not Continue")]
    public async Task ContinueInterruptedThrows()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "gate",
                static (context, _) => context.ResumePayload is null
                    ? Task.FromResult<NodeResult>(NodeResult.Interrupt("stop"))
                    : Task.FromResult<NodeResult>(NodeResult.Continue()))
            .AddEdge(GraphConstants.Start, "gate")
            .AddEdge("gate", GraphConstants.End)
            .Compile(checkpointer);

        await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "cont-int", StreamMode = StreamMode.Events });

        var exception = await Should.ThrowAsync<GraphInvalidContinueException>(
            async () => await graph.ContinueInvokeAsync("cont-int"));

        exception.Code.ShouldBe(VolutaErrorCodes.GraphInvalidContinue);
    }

    [Fact(DisplayName = "Given linear graph paused via fork at Running step, when ContinueInvoke, then reaches Done")]
    public async Task ForkRunningThenContinueCompletes()
    {
        var checkpointer = new InMemoryCheckpointer();
        // Three nodes so history has a Running snapshot with next nodes mid-way.
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "a",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "a"))))
            .AddNode(
                "b",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "b"))))
            .AddNode(
                "c",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "c"))))
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", "b")
            .AddEdge("b", "c")
            .AddEdge("c", GraphConstants.End)
            .Compile(checkpointer);

        await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "full-run", StreamMode = StreamMode.Values });

        var history = await graph.GetHistoryAsync("full-run");
        var running = history.LastOrDefault(static item =>
            item.Status == GraphRunStatus.Running && item.NextNodes.Count > 0);
        running.ShouldNotBeNull("expected a Running checkpoint with next nodes in history");

        await graph.ForkAsync("full-run", running.Step, "cont-child");

        // Optional host patch before continue (Append reducer).
        await graph.UpdateStateAsync(
            "cont-child",
            [new ChannelWrite("messages", "patched")]);

        var afterUpdate = await graph.GetStateAsync("cont-child");
        afterUpdate.ShouldNotBeNull();
        afterUpdate.Status.ShouldBe(GraphRunStatus.Running);

        var terminal = await graph.ContinueInvokeAsync("cont-child");

        terminal.Kind.ShouldBe(StreamEventKind.End);
        var final = await graph.GetStateAsync("cont-child");
        final.ShouldNotBeNull();
        final.Status.ShouldBe(GraphRunStatus.Done);
        var messages = final.Values["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldContain("patched");
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
