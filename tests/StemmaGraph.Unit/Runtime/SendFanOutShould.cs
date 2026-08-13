using Shouldly;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Abstractions.Results;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Abstractions.Streaming;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph.Builder;
using Xunit;

namespace StemmaGraph.Unit.Runtime;

public sealed class SendFanOutShould
{
    [Fact(DisplayName = "Given map node emits Sends, when run, then worker runs once per item and Append merges")]
    public async Task MapStyleSendFanOut()
    {
        var graph = new StateGraph()
            .AddChannel("items", ChannelKind.Append)
            .AddChannel("results", ChannelKind.Append)
            .AddNode(
                "map",
                static (_, _) =>
                {
                    var sends = new Send[]
                    {
                        new("worker", "a"),
                        new("worker", "b"),
                        new("worker", "c"),
                    };
                    return Task.FromResult<NodeResult>(NodeResult.ContinueWithSends(sends));
                })
            .AddNode(
                "worker",
                static (context, _) =>
                {
                    var payload = context.TaskPayload?.ToString() ?? "?";
                    return Task.FromResult<NodeResult>(
                        NodeResult.Continue(new ChannelWrite("results", payload.ToUpperInvariant())));
                })
            .AddEdge(GraphConstants.Start, "map")
            .AddEdge("map", GraphConstants.End)
            .AddEdge("worker", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "send-1", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        var results = terminal.State!["results"].ShouldBeOfType<List<object?>>();
        results.OrderBy(static item => item?.ToString()).ShouldBe(["A", "B", "C"]);
    }
}
