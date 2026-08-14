using System.Diagnostics.Metrics;
using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Diagnostics;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Diagnostics;
using Voluta.Exceptions.Run;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Runtime;

public sealed class ProductionHardeningShould
{
    [Fact(DisplayName = "Given Running checkpoint with PendingSends only, when Continue, then does not re-pull NextNodes side-effect")]
    public async Task ContinuePendingSendsDoesNotReDriveNextNodes()
    {
        var mapRuns = 0;
        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("results", ChannelKind.Append)
            .AddNode(
                "map",
                (_, _) =>
                {
                    Interlocked.Increment(ref mapRuns);
                    return Task.FromResult<NodeResult>(
                        NodeResult.ContinueWithSends(
                            new Send("worker", "a"),
                            new Send("worker", "b")));
                })
            .AddNode(
                "worker",
                static (context, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(
                        new ChannelWrite("results", context.TaskPayload?.ToString()))))
            .AddEdge(GraphConstants.Start, "map")
            .AddEdge("map", GraphConstants.End)
            .AddEdge("worker", GraphConstants.End)
            .Compile(checkpointer);

        // Simulate crash after map applied + scheduled sends (Running, incomplete workers).
        await checkpointer.PutAsync(
            new CheckpointSnapshot
            {
                ThreadId = "a2-continue-1",
                Step = 1,
                Status = GraphRunStatus.Running,
                ChannelValues = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["map_hits"] = 1,
                    ["results"] = new List<object?>(),
                },
                NextNodes = ["map", "worker"],
                PendingSends =
                [
                    new PendingSend { NodeName = "worker", TaskId = "map->worker:0", Payload = "a" },
                    new PendingSend { NodeName = "worker", TaskId = "map->worker:1", Payload = "b" },
                ],
                LastNode = "map",
            });

        mapRuns = 0;
        var terminal = await graph.ContinueInvokeAsync("a2-continue-1");

        terminal.Kind.ShouldBe(StreamEventKind.End);
        mapRuns.ShouldBe(0);
        var done = await checkpointer.GetAsync("a2-continue-1");
        done!.Status.ShouldBe(GraphRunStatus.Done);
        var results = done.ChannelValues["results"].ShouldBeOfType<List<object?>>();
        results.OrderBy(static item => item?.ToString()).ShouldBe(["a", "b"]);
    }

    [Fact(DisplayName = "Given two interrupts, when partial Resumes then second Resumes, then Done")]
    public async Task ProgressiveMultiInterruptResume()
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

        await graph.InvokeAsync([], new RunOptions { ThreadId = "b2-partial-1" });
        var snap = await checkpointer.GetAsync("b2-partial-1");
        snap!.PendingInterrupts.Count.ShouldBe(2);

        var firstId = snap.PendingInterrupts[0].TaskId;
        var secondId = snap.PendingInterrupts[1].TaskId;
        var firstPayload = snap.PendingInterrupts[0].TaskPayload?.ToString();

        var mid = await graph.ResumeInvokeAsync(
            "b2-partial-1",
            Command.ApproveResumes(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [firstId] = "ok-1",
            }));

        mid.Kind.ShouldBe(StreamEventKind.Interrupt);
        var still = await checkpointer.GetAsync("b2-partial-1");
        still!.Status.ShouldBe(GraphRunStatus.Interrupted);
        still.PendingInterrupts.Count.ShouldBe(1);
        still.PendingInterrupts[0].TaskId.ShouldBe(secondId);

        var resultsMid = still.ChannelValues.TryGetValue("results", out var raw) && raw is List<object?> list
            ? list
            : [];
        resultsMid.ShouldContain($"{firstPayload}:ok-1");

        var doneEvent = await graph.ResumeInvokeAsync(
            "b2-partial-1",
            Command.ApproveResumes(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [secondId] = "ok-2",
            }));
        doneEvent.Kind.ShouldBe(StreamEventKind.End);

        var done = await checkpointer.GetAsync("b2-partial-1");
        done!.Status.ShouldBe(GraphRunStatus.Done);
        done.PendingInterrupts.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Given multi-interrupt, when Resumes has unknown task id, then invalid payload")]
    public async Task ProgressiveMultiInterruptUnknownTaskIdFails()
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

        await graph.InvokeAsync([], new RunOptions { ThreadId = "b2-unknown-1" });

        var exception = await Should.ThrowAsync<GraphInvalidCommandException>(async () =>
        {
            await graph.ResumeInvokeAsync(
                "b2-unknown-1",
                Command.ApproveResumes(new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["not-a-real-task"] = "x",
                }));
        });

        exception.Code.ShouldBe(VolutaErrorCodes.CommandInvalidPayload);
    }

    [Fact(DisplayName = "Given flood of custom stream events, when buffer full, then StreamDropped increments")]
    public async Task StreamBackpressureDropsAndCounts()
    {
        using var listener = new MeterListener();
        long dropped = 0;
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == VolutaDiagnostics.SourceName
                && instrument.Name == VolutaDiagnostics.StreamDroppedMetricName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == VolutaDiagnostics.StreamDroppedMetricName)
            {
                Interlocked.Add(ref dropped, measurement);
            }
        });
        listener.Start();

        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddNode(
                "flood",
                static async (context, cancellationToken) =>
                {
                    for (var index = 0; index < 2000; index++)
                    {
                        await context.Stream!.WriteCustomAsync($"n-{index}", cancellationToken);
                    }

                    return NodeResult.Continue();
                })
            .AddEdge(GraphConstants.Start, "flood")
            .AddEdge("flood", GraphConstants.End)
            .Compile(checkpointer);

        var events = new List<StreamEvent>();
        await foreach (var item in graph.StreamAsync(
                           [],
                           new RunOptions { ThreadId = "c2-drop-1", StreamMode = StreamMode.Events }))
        {
            events.Add(item);
        }

        listener.RecordObservableInstruments();
        dropped.ShouldBeGreaterThan(0);
        events.ShouldContain(static item => item.Kind == StreamEventKind.End);
    }
}
