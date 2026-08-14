using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
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
}
