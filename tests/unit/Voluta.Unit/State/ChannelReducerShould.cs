using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.State;

public sealed class ChannelReducerShould
{
    [Fact(DisplayName = "Given Append channel, when node writes a list then a string, then list is flattened and string is one element")]
    public async Task AppendFlattensEnumerableNotString()
    {
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "a",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", new List<object?> { "a", "b" }))))
            .AddNode(
                "b",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "c"))))
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", "b")
            .AddEdge("b", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "append-1", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        var messages = terminal.State!["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldBe(["a", "b", "c"]);
    }

    [Fact(DisplayName = "Given LastValue channel, when two sequential nodes write, then final value is the later write")]
    public async Task LastValueSequentialOverwrite()
    {
        var graph = new StateGraph()
            .AddChannel("status", ChannelKind.LastValue)
            .AddNode(
                "a",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("status", "from-a"))))
            .AddNode(
                "b",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("status", "from-b"))))
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", "b")
            .AddEdge("b", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "lv-seq-1", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        terminal.State!["status"].ShouldBe("from-b");
    }
}
