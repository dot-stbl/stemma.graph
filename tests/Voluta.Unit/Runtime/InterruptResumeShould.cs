using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Exceptions.Run;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Runtime;

public sealed class InterruptResumeShould
{
    [Fact(DisplayName = "Given interrupt node, when Invoke then Resume, then continues to End")]
    public async Task InterruptThenResumeContinues()
    {
        var checkpointer = new InMemoryCheckpointer();
        var phase = 0;

        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "gate",
                (context, _) =>
                {
                    phase++;
                    return context.ResumePayload is null && phase == 1
                        ? Task.FromResult<NodeResult>(NodeResult.Interrupt(new { amount = 50 }))
                        : Task.FromResult<NodeResult>(
                            NodeResult.Continue(new ChannelWrite("messages", "approved")));
                })
            .AddEdge(GraphConstants.Start, "gate")
            .AddEdge("gate", GraphConstants.End)
            .Compile(checkpointer);

        var interrupted = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "hitl-1", StreamMode = StreamMode.Events });

        interrupted.Kind.ShouldBe(StreamEventKind.Interrupt);
        var snapshot = await checkpointer.GetAsync("hitl-1");
        snapshot!.Status.ShouldBe(GraphRunStatus.Interrupted);

        var terminal = await graph.ResumeInvokeAsync(
            "hitl-1",
            new Command { Kind = "approve", Payload = "ok" });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        var done = await checkpointer.GetAsync("hitl-1");
        done!.Status.ShouldBe(GraphRunStatus.Done);
        var messages = done.ChannelValues["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldContain("approved");
    }

    [Fact(DisplayName = "Given done thread, when ResumeAsync is called, then fails invalid resume")]
    public async Task ResumeWhenDoneFails()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddNode(
                "a",
                static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue()))
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", GraphConstants.End)
            .Compile(checkpointer);

        var terminal = await graph.InvokeAsync([], new RunOptions { ThreadId = "done-1" });
        terminal.Kind.ShouldBe(StreamEventKind.End);

        await Should.ThrowAsync<GraphInvalidResumeException>(async () =>
        {
            await foreach (var _ in graph.ResumeAsync("done-1", new Command { Kind = "approve" }))
            {
            }
        });
    }

    [Fact(DisplayName = "Given unknown thread, when ResumeAsync is called, then fails invalid resume")]
    public async Task ResumeUnknownThreadFails()
    {
        var graph = new StateGraph()
            .AddNode(
                "gate",
                static (_, _) => Task.FromResult<NodeResult>(NodeResult.Interrupt("wait")))
            .AddEdge(GraphConstants.Start, "gate")
            .AddEdge("gate", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        await Should.ThrowAsync<GraphInvalidResumeException>(async () =>
        {
            await graph.ResumeInvokeAsync("missing-thread", new Command { Kind = "approve" });
        });
    }

    [Fact(DisplayName = "Given interrupted gate, when Resume with Payload, then gate sees ResumePayload")]
    public async Task ResumePayloadReachesNode()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "gate",
                (context, _) => context.ResumePayload is null
                    ? Task.FromResult<NodeResult>(NodeResult.Interrupt(new { need = "signoff" }))
                    : Task.FromResult<NodeResult>(
                        NodeResult.Continue(
                            new ChannelWrite("messages", $"payload={context.ResumePayload}"))))
            .AddEdge(GraphConstants.Start, "gate")
            .AddEdge("gate", GraphConstants.End)
            .Compile(checkpointer);

        var interrupted = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "payload-1", StreamMode = StreamMode.Events });
        interrupted.Kind.ShouldBe(StreamEventKind.Interrupt);

        var terminal = await graph.ResumeInvokeAsync(
            "payload-1",
            new Command { Kind = "approve", Payload = "signed-off" });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        var done = await checkpointer.GetAsync("payload-1");
        done!.Status.ShouldBe(GraphRunStatus.Done);
        var messages = done.ChannelValues["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldContain("payload=signed-off");
    }

    [Fact(DisplayName = "Given interrupted gate, when Resume with Command.Values, then channel values apply before gate re-runs")]
    public async Task ResumeAppliesCommandValues()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddChannel("decision", ChannelKind.LastValue)
            .AddNode(
                "gate",
                (context, _) => context.ResumePayload is null
                    ? Task.FromResult<NodeResult>(NodeResult.Interrupt("need-decision"))
                    : Task.FromResult<NodeResult>(
                        NodeResult.Continue(
                            new ChannelWrite(
                                "messages",
                                $"decision={context.Read<string>("decision") ?? "(none)"}"))))
            .AddEdge(GraphConstants.Start, "gate")
            .AddEdge("gate", GraphConstants.End)
            .Compile(checkpointer);

        var interrupted = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "values-1", StreamMode = StreamMode.Events });
        interrupted.Kind.ShouldBe(StreamEventKind.Interrupt);

        var terminal = await graph.ResumeInvokeAsync(
            "values-1",
            new Command
            {
                Kind = "approve",
                Payload = "ok",
                Values = new Dictionary<string, object?> { ["decision"] = "go" },
            });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        var done = await checkpointer.GetAsync("values-1");
        done!.Status.ShouldBe(GraphRunStatus.Done);
        var messages = done.ChannelValues["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldContain("decision=go");
        done.ChannelValues["decision"].ShouldBe("go");
    }

    [Fact(DisplayName = "Given interrupt node, when Invoke with Events, then Interrupt event carries payload")]
    public async Task InterruptEventCarriesPayload()
    {
        var graph = new StateGraph()
            .AddNode(
                "gate",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Interrupt(new { amount = 50, currency = "USD" })))
            .AddEdge(GraphConstants.Start, "gate")
            .AddEdge("gate", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "evt-int-1", StreamMode = StreamMode.Events });

        terminal.Kind.ShouldBe(StreamEventKind.Interrupt);
        terminal.NodeNames.ShouldBe(["gate"]);
        terminal.Payload.ShouldNotBeNull();
        var payloadText = terminal.Payload!.ToString();
        payloadText.ShouldNotBeNull();
        payloadText.ShouldContain("amount");
    }
}
