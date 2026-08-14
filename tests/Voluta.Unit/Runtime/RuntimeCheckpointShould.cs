using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Runtime;

public sealed class RuntimeCheckpointShould
{
    [Fact(DisplayName = "Given multi-node linear run, when ListAsync is called, then snapshots are ordered by step with Done terminal")]
    public async Task MultiNodeListAsyncHistory()
    {
        var checkpointer = new InMemoryCheckpointer();
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
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", "b")
            .AddEdge("b", GraphConstants.End)
            .Compile(checkpointer);

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "hist-1", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);

        var list = await checkpointer.ListAsync("hist-1");
        list.Count.ShouldBeGreaterThanOrEqualTo(1);
        for (var index = 1; index < list.Count; index++)
        {
            list[index].Step.ShouldBeGreaterThanOrEqualTo(list[index - 1].Step);
        }

        list[^1].Status.ShouldBe(GraphRunStatus.Done);
        var messages = list[^1].ChannelValues["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldContain("a");
        messages.ShouldContain("b");
    }
}
