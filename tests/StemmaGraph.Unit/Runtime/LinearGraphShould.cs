using Shouldly;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Abstractions.Results;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Abstractions.Streaming;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph.Builder;
using Xunit;

namespace StemmaGraph.Unit.Runtime;

public sealed class LinearGraphShould
{
    [Fact(DisplayName = "Given START→A→END, when InvokeAsync is called, then reaches End with A write applied")]
    public async Task RunLinearChainToEnd()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "a",
                static (context, _) =>
                {
                    var existing = context.Read<List<object?>>("messages") ?? [];
                    return Task.FromResult<NodeResult>(
                        NodeResult.Continue(new ChannelWrite("messages", new List<object?> { "from-a" })));
                })
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", GraphConstants.End)
            .Compile(checkpointer);

        var terminal = await graph.InvokeAsync(
            [new ChannelWrite("messages", new List<object?> { "seed" })],
            new RunOptions { ThreadId = "linear-1", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        terminal.State.ShouldNotBeNull();
        var messages = terminal.State!["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldContain("seed");
        messages.ShouldContain("from-a");

        var snapshot = await checkpointer.GetAsync("linear-1");
        snapshot!.Status.ShouldBe(GraphRunStatus.Done);
    }
}
