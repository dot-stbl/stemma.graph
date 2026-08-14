using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Runtime;

public sealed class CustomStreamEventsShould
{
    [Fact(DisplayName = "Given node writes custom payload, when StreamAsync Events, then emits Custom before End")]
    public async Task EmitCustomEventsDuringNode()
    {
        var graph = new StateGraph()
            .AddNode(
                "progress",
                static async (context, cancellationToken) =>
                {
                    await context.Stream.WriteCustomAsync(new { phase = "start" }, cancellationToken);
                    await context.Stream.WriteCustomAsync("halfway", cancellationToken);
                    return NodeResult.Continue();
                })
            .AddEdge(GraphConstants.Start, "progress")
            .AddEdge("progress", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var events = new List<StreamEvent>();
        await foreach (var item in graph.StreamAsync(
                           [],
                           new RunOptions { ThreadId = "custom-1", StreamMode = StreamMode.Events }))
        {
            events.Add(item);
        }

        var customs = events.Where(static item => item.Kind == StreamEventKind.Custom).ToList();
        customs.Count.ShouldBe(2);
        customs[0].NodeNames.ShouldContain("progress");
        customs[0].Payload.ShouldNotBeNull();
        customs[1].Payload.ShouldBe("halfway");
        events.Last().Kind.ShouldBe(StreamEventKind.End);
    }

    [Fact(DisplayName = "Given node writes messages, when StreamAsync Messages, then sequence is Start, Messages, End")]
    public async Task MessagesModeEmitsTokensWithoutUpdates()
    {
        var graph = new StateGraph()
            .AddChannel("out", ChannelKind.LastValue)
            .AddNode(
                "tokens",
                static async (context, cancellationToken) =>
                {
                    await context.Stream.WriteMessageAsync("hel", cancellationToken);
                    await context.Stream.WriteMessageAsync("lo", cancellationToken);
                    return NodeResult.Continue(new ChannelWrite("out", "hello"));
                })
            .AddEdge(GraphConstants.Start, "tokens")
            .AddEdge("tokens", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var events = new List<StreamEvent>();
        await foreach (var item in graph.StreamAsync(
                           [],
                           new RunOptions { ThreadId = "msg-1", StreamMode = StreamMode.Messages }))
        {
            events.Add(item);
        }

        events[0].Kind.ShouldBe(StreamEventKind.Start);
        events.ShouldNotContain(static item => item.Kind == StreamEventKind.Updates);
        events.ShouldNotContain(static item => item.Kind == StreamEventKind.Values);
        var messages = events.Where(static item => item.Kind == StreamEventKind.Messages).ToList();
        messages.Select(static item => item.Payload).ShouldBe(["hel", "lo"]);
        events.Last().Kind.ShouldBe(StreamEventKind.End);
    }

    [Fact(DisplayName = "Given node writes messages, when StreamAsync Updates, then tokens appear alongside Updates")]
    public async Task UpdatesModeStillForwardsMessages()
    {
        var graph = new StateGraph()
            .AddChannel("out", ChannelKind.LastValue)
            .AddNode(
                "tokens",
                static async (context, cancellationToken) =>
                {
                    await context.Stream.WriteMessageAsync("x", cancellationToken);
                    return NodeResult.Continue(new ChannelWrite("out", "x"));
                })
            .AddEdge(GraphConstants.Start, "tokens")
            .AddEdge("tokens", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var events = new List<StreamEvent>();
        await foreach (var item in graph.StreamAsync(
                           [],
                           new RunOptions { ThreadId = "upd-msg-1", StreamMode = StreamMode.Updates }))
        {
            events.Add(item);
        }

        events.ShouldContain(static item => item.Kind == StreamEventKind.Messages);
        events.ShouldContain(static item => item.Kind == StreamEventKind.Updates);
        events.Last().Kind.ShouldBe(StreamEventKind.End);
    }
}
