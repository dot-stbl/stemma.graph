using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Runtime;

public sealed class ThreadIsolationAndCancelShould
{
    [Fact(DisplayName = "Given two thread ids, when invoked, then channel values do not leak")]
    public async Task IsolateThreads()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("v", ChannelKind.LastValue)
            .AddNode(
                "a",
                static (context, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("v", context.Read<string>("v") + "-ran"))))
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", GraphConstants.End)
            .Compile(checkpointer);

        var t1 = await graph.InvokeAsync(
            [new ChannelWrite("v", "one")],
            new RunOptions { ThreadId = "iso-1", StreamMode = StreamMode.Values });
        var t2 = await graph.InvokeAsync(
            [new ChannelWrite("v", "two")],
            new RunOptions { ThreadId = "iso-2", StreamMode = StreamMode.Values });

        t1.State!["v"].ShouldBe("one-ran");
        t2.State!["v"].ShouldBe("two-ran");

        var s1 = await checkpointer.GetAsync("iso-1");
        var s2 = await checkpointer.GetAsync("iso-2");
        s1!.ChannelValues["v"].ShouldBe("one-ran");
        s2!.ChannelValues["v"].ShouldBe("two-ran");
    }

    [Fact(DisplayName = "Given cancelled stream token, when node observes it, then run stops")]
    public async Task StreamCancelStops()
    {
        using var cts = new CancellationTokenSource();
        var graph = new StateGraph()
            .AddNode(
                "slow",
                async (_, cancellationToken) =>
                {
                    await cts.CancelAsync();
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    return NodeResult.Continue();
                })
            .AddEdge(GraphConstants.Start, "slow")
            .AddEdge("slow", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in graph.StreamAsync(
                               [],
                               new RunOptions { ThreadId = "cancel-1" },
                               cts.Token))
            {
            }
        });
    }
}
