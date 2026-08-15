using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Runtime;

public sealed class StreamModesShould
{
    [Fact(DisplayName = "Given linear graph, when StreamAsync with Updates, then emits Updates with node names and writes")]
    public async Task UpdatesEmitsNodeWrites()
    {
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "a",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "from-a"))))
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var events = new List<StreamEvent>();
        await foreach (var item in graph.StreamAsync(
                           [],
                           new RunOptions { ThreadId = "upd-1", StreamMode = StreamMode.Updates }))
        {
            events.Add(item);
        }

        var updates = events.Where(static item => item.Kind == StreamEventKind.Updates).ToList();
        updates.ShouldNotBeEmpty();
        updates.ShouldContain(static item =>
            item.NodeNames.Contains("a")
            && item.Writes.Any(write => write.ChannelName == "messages" && Equals(write.Value, "from-a")));
        events.Last().Kind.ShouldBe(StreamEventKind.End);
    }

    [Fact(DisplayName = "Given linear graph, when StreamAsync with Events, then sequence includes Start and End")]
    public async Task EventsIncludesStartAndEnd()
    {
        var graph = new StateGraph()
            .AddNode(
                "a",
                static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue()))
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var kinds = new List<StreamEventKind>();
        await foreach (var item in graph.StreamAsync(
                           [],
                           new RunOptions { ThreadId = "evt-1", StreamMode = StreamMode.Events }))
        {
            kinds.Add(item.Kind);
        }

        kinds.ShouldContain(StreamEventKind.Start);
        kinds.ShouldContain(StreamEventKind.End);
        kinds[0].ShouldBe(StreamEventKind.Start);
        kinds[^1].ShouldBe(StreamEventKind.End);
    }
}
