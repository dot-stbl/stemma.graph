using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Diagnostics;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Exceptions.Run;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Runtime;

public sealed class MultiInterruptResumeShould
{
    [Fact(DisplayName = "Given two parallel Send interrupts, when Resume with Resumes map, then both continue")]
    public async Task TwoParallelSendInterruptsResumeByTaskId()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("results", ChannelKind.Append)
            .AddNode(
                "map",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.ContinueWithSends(
                        new Send("worker", "alpha"),
                        new Send("worker", "beta"))))
            .AddNode(
                "worker",
                static (context, _) => Task.FromResult<NodeResult>(
                    context.ResumePayload is null
                        ? NodeResult.Interrupt(new { item = context.TaskPayload?.ToString() })
                        : NodeResult.Continue(
                            new ChannelWrite(
                                "results",
                                $"{context.TaskPayload}:{context.ResumePayload}"))))
            .AddEdge(GraphConstants.Start, "map")
            .AddEdge("map", GraphConstants.End)
            .AddEdge("worker", GraphConstants.End)
            .Compile(checkpointer);

        var interrupted = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "multi-1", StreamMode = StreamMode.Events });

        interrupted.Kind.ShouldBe(StreamEventKind.Interrupt);
        var snapshot = await checkpointer.GetAsync("multi-1");
        snapshot!.Status.ShouldBe(GraphRunStatus.Interrupted);
        snapshot.PendingInterrupts.Count.ShouldBe(2);
        snapshot.PendingInterrupts.Select(static item => item.TaskPayload?.ToString())
            .OrderBy(static value => value)
            .ShouldBe(["alpha", "beta"]);

        var resumes = snapshot.PendingInterrupts.ToDictionary(
            static item => item.TaskId,
            static item => (object?)$"ok-{item.TaskPayload}",
            StringComparer.Ordinal);

        var terminal = await graph.ResumeInvokeAsync("multi-1", Command.ApproveResumes(resumes));

        terminal.Kind.ShouldBe(StreamEventKind.End);
        var done = await checkpointer.GetAsync("multi-1");
        done!.Status.ShouldBe(GraphRunStatus.Done);
        var results = done.ChannelValues["results"].ShouldBeOfType<List<object?>>();
        results.OrderBy(static item => item?.ToString()).ShouldBe(["alpha:ok-alpha", "beta:ok-beta"]);
    }

    [Fact(DisplayName = "Given two parallel interrupts, when Resume without Resumes map, then fails invalid payload")]
    public async Task MultiInterruptWithoutResumesMapFails()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddNode(
                "map",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.ContinueWithSends(
                        new Send("worker", "a"),
                        new Send("worker", "b"))))
            .AddNode(
                "worker",
                static (context, _) => context.ResumePayload is null
                    ? Task.FromResult<NodeResult>(NodeResult.Interrupt(context.TaskPayload))
                    : Task.FromResult<NodeResult>(NodeResult.Continue()))
            .AddEdge(GraphConstants.Start, "map")
            .AddEdge("map", GraphConstants.End)
            .AddEdge("worker", GraphConstants.End)
            .Compile(checkpointer);

        await graph.InvokeAsync([], new RunOptions { ThreadId = "multi-fail-1" });

        var exception = await Should.ThrowAsync<GraphInvalidCommandException>(async () =>
        {
            await graph.ResumeInvokeAsync("multi-fail-1", Command.Approve("ok"));
        });

        exception.Code.ShouldBe(VolutaErrorCodes.CommandInvalidPayload);
        var still = await checkpointer.GetAsync("multi-fail-1");
        still!.Status.ShouldBe(GraphRunStatus.Interrupted);
        still.PendingInterrupts.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Given two static parallel interrupt nodes, when Resume with Resumes, then both continue")]
    public async Task TwoParallelPullInterruptsResumeByNodeTaskId()
    {
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("results", ChannelKind.Append)
            .AddNode(
                "left",
                static (context, _) => context.ResumePayload is null
                    ? Task.FromResult<NodeResult>(NodeResult.Interrupt("left-need"))
                    : Task.FromResult<NodeResult>(
                        NodeResult.Continue(new ChannelWrite("results", $"left:{context.ResumePayload}"))))
            .AddNode(
                "right",
                static (context, _) => context.ResumePayload is null
                    ? Task.FromResult<NodeResult>(NodeResult.Interrupt("right-need"))
                    : Task.FromResult<NodeResult>(
                        NodeResult.Continue(new ChannelWrite("results", $"right:{context.ResumePayload}"))))
            .AddEdge(GraphConstants.Start, "left")
            .AddEdge(GraphConstants.Start, "right")
            .AddEdge("left", GraphConstants.End)
            .AddEdge("right", GraphConstants.End)
            .Compile(checkpointer);

        var interrupted = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "multi-pull-1", StreamMode = StreamMode.Events });
        interrupted.Kind.ShouldBe(StreamEventKind.Interrupt);

        var snapshot = await checkpointer.GetAsync("multi-pull-1");
        snapshot!.PendingInterrupts.Count.ShouldBe(2);

        var resumes = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["left"] = "L",
            ["right"] = "R",
        };

        var terminal = await graph.ResumeInvokeAsync("multi-pull-1", Command.ApproveResumes(resumes));
        terminal.Kind.ShouldBe(StreamEventKind.End);

        var done = await checkpointer.GetAsync("multi-pull-1");
        var results = done!.ChannelValues["results"].ShouldBeOfType<List<object?>>();
        results.OrderBy(static item => item?.ToString()).ShouldBe(["left:L", "right:R"]);
    }
}
