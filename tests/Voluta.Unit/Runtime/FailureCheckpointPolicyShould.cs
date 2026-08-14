using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Exceptions;
using Voluta.Exceptions.Run;
using Voluta.Graph.Builder;
using Voluta.Graph.Options;
using Xunit;

namespace Voluta.Unit.Runtime;

public sealed class FailureCheckpointPolicyShould
{
    [Fact(DisplayName =
        "Given success then node throw, when GetAsync is called, then status is Failed with last-good channels")]
    public async Task GetReturnsFailedWithLastGoodChannelsAfterNodeThrow()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "ok",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "committed"))))
            .AddNode(
                "boom",
                static (_, _) => throw new InvalidOperationException("node failed"))
            .AddEdge(GraphConstants.Start, "ok")
            .AddEdge("ok", "boom")
            .AddEdge("boom", GraphConstants.End)
            .Compile(checkpointer);

        var exception = await Should.ThrowAsync<GraphRunFailedException>(async () =>
        {
            await graph.InvokeAsync(
                [],
                new RunOptions { ThreadId = "fail-node-1", StreamMode = StreamMode.Events });
        });

        exception.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("node failed");

        var latest = await checkpointer.GetAsync("fail-node-1");
        latest.ShouldNotBeNull();
        latest!.Status.ShouldBe(GraphRunStatus.Failed);
        latest.Step.ShouldBeGreaterThan(0);

        var messages = latest.ChannelValues["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldContain("committed");
        messages.ShouldNotContain("from-boom");

        var history = await checkpointer.ListAsync("fail-node-1");
        history.ShouldContain(snapshot => snapshot.Status == GraphRunStatus.Running);
        var lastGood = history.Last(snapshot => snapshot.Status == GraphRunStatus.Running);
        lastGood.Step.ShouldBeLessThan(latest.Step);
        lastGood.ChannelValues["messages"].ShouldBeOfType<List<object?>>().ShouldContain("committed");
    }

    [Fact(DisplayName =
        "Given success then node throw, when StreamAsync is enumerated, then Failed event precedes exception")]
    public async Task StreamSurfacesFailedEventBeforeThrow()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("v", ChannelKind.LastValue)
            .AddNode(
                "ok",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("v", "good"))))
            .AddNode(
                "boom",
                static (_, _) => throw new InvalidOperationException("stream boom"))
            .AddEdge(GraphConstants.Start, "ok")
            .AddEdge("ok", "boom")
            .AddEdge("boom", GraphConstants.End)
            .Compile(checkpointer);

        StreamEvent? failedEvent = null;
        var thrown = await Should.ThrowAsync<GraphRunFailedException>(async () =>
        {
            await foreach (var item in graph.StreamAsync(
                               [],
                               new RunOptions { ThreadId = "fail-stream-1", StreamMode = StreamMode.Events }))
            {
                if (item.Kind == StreamEventKind.Failed)
                {
                    failedEvent = item;
                }
            }
        });

        thrown.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("stream boom");
        failedEvent.ShouldNotBeNull();
        failedEvent!.Kind.ShouldBe(StreamEventKind.Failed);

        var latest = await checkpointer.GetAsync("fail-stream-1");
        latest!.Status.ShouldBe(GraphRunStatus.Failed);
        latest.ChannelValues["v"].ShouldBe("good");
    }

    [Fact(DisplayName =
        "Given infinite cycle over limit, when InvokeAsync fails, then Get keeps last successful channels")]
    public async Task OutOfStepsPreservesLastGoodChannels()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("n", ChannelKind.LastValue)
            .AddNode(
                "loop",
                static (context, _) =>
                {
                    var current = context.Read<int?>("n") ?? 0;
                    return Task.FromResult<NodeResult>(
                        NodeResult.Continue(new ChannelWrite("n", current + 1)));
                })
            .AddEdge(GraphConstants.Start, "loop")
            .AddEdge("loop", "loop")
            .Compile(checkpointer, new CompileOptions { RecursionLimit = 3 });

        await Should.ThrowAsync<GraphOutOfStepsException>(async () =>
        {
            await graph.InvokeAsync(
                [new ChannelWrite("n", 0)],
                new RunOptions { ThreadId = "fail-oos-1", StreamMode = StreamMode.Values });
        });

        var latest = await checkpointer.GetAsync("fail-oos-1");
        latest.ShouldNotBeNull();
        latest!.Status.ShouldBe(GraphRunStatus.Failed);
        latest.ChannelValues["n"].ShouldBeOfType<int>().ShouldBeGreaterThan(0);

        var history = await checkpointer.ListAsync("fail-oos-1");
        history.ShouldContain(snapshot => snapshot.Status == GraphRunStatus.Running);
        history.Last().Status.ShouldBe(GraphRunStatus.Failed);
    }

    [Fact(DisplayName =
        "Given failed thread, when ResumeInvokeAsync is called, then rejects as not interrupted")]
    public async Task ResumeRejectsFailedThread()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddNode(
                "boom",
                static (_, _) => throw new InvalidOperationException("no resume"))
            .AddEdge(GraphConstants.Start, "boom")
            .AddEdge("boom", GraphConstants.End)
            .Compile(checkpointer);

        await Should.ThrowAsync<GraphRunFailedException>(async () =>
        {
            await graph.InvokeAsync([], new RunOptions { ThreadId = "fail-resume-1" });
        });

        var resumeException = await Should.ThrowAsync<GraphInvalidResumeException>(async () =>
        {
            await graph.ResumeInvokeAsync(
                "fail-resume-1",
                Command.Approve());
        });

        resumeException.Message.ShouldContain("not interrupted");
    }

    [Fact(DisplayName =
        "Given concurrent LastValue multi-write, when Invoke fails, then Get is Failed with empty last-good channels")]
    public async Task ConcurrentLastValueFailureMarksFailedWithLastGood()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("status", ChannelKind.LastValue)
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "seed",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "before-conflict"))))
            .AddNode(
                "left",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("status", "L"))))
            .AddNode(
                "right",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("status", "R"))))
            .AddEdge(GraphConstants.Start, "seed")
            .AddEdge("seed", "left")
            .AddEdge("seed", "right")
            .AddEdge("left", GraphConstants.End)
            .AddEdge("right", GraphConstants.End)
            .Compile(checkpointer);

        await Should.ThrowAsync<GraphConcurrentUpdateException>(async () =>
        {
            await graph.InvokeAsync(
                [],
                new RunOptions { ThreadId = "fail-lv-1", StreamMode = StreamMode.Events });
        });

        var latest = await checkpointer.GetAsync("fail-lv-1");
        latest.ShouldNotBeNull();
        latest!.Status.ShouldBe(GraphRunStatus.Failed);
        latest.ChannelValues["status"].ShouldBeNull();
        var messages = latest.ChannelValues["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldContain("before-conflict");

        var history = await checkpointer.ListAsync("fail-lv-1");
        history.ShouldContain(snapshot => snapshot.Status == GraphRunStatus.Running);
        history.Last().Status.ShouldBe(GraphRunStatus.Failed);
    }

    [Fact(DisplayName =
        "Given cancelled mid-run after first node, when stream stops, then checkpoint keeps last-good Running not Failed")]
    public async Task CancelMidRunDoesNotMarkFailed()
    {
        using var cts = new CancellationTokenSource();
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "first",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "first-ok"))))
            .AddNode(
                "second",
                async (_, cancellationToken) =>
                {
                    await cts.CancelAsync();
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    return NodeResult.Continue(new ChannelWrite("messages", "second-ok"));
                })
            .AddEdge(GraphConstants.Start, "first")
            .AddEdge("first", "second")
            .AddEdge("second", GraphConstants.End)
            .Compile(checkpointer);

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in graph.StreamAsync(
                               [],
                               new RunOptions { ThreadId = "fail-cancel-1", StreamMode = StreamMode.Events },
                               cts.Token))
            {
            }
        });

        var latest = await checkpointer.GetAsync("fail-cancel-1");
        latest.ShouldNotBeNull();
        latest!.Status.ShouldNotBe(GraphRunStatus.Failed);
        latest.Status.ShouldNotBe(GraphRunStatus.Done);
        var messages = latest.ChannelValues["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldContain("first-ok");
        messages.ShouldNotContain("second-ok");
    }
}
