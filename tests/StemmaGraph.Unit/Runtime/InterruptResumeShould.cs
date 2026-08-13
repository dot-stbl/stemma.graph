using Shouldly;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Abstractions.Results;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Abstractions.Streaming;
using StemmaGraph.Checkpoint;
using StemmaGraph.Exceptions.Run;
using StemmaGraph.Graph.Builder;
using Xunit;

namespace StemmaGraph.Unit.Runtime;

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
}
