using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Runtime;

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
