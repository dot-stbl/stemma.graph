using System.Diagnostics;
using System.Diagnostics.Metrics;
using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Diagnostics;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Diagnostics;

public sealed class VolutaDiagnosticsShould
{
    [Fact(DisplayName = "Given ActivityListener, when linear graph runs, then superstep and node activities start")]
    public async Task EmitSuperstepAndNodeActivities()
    {
        var started = new List<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == VolutaDiagnostics.SourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => started.Add(activity.OperationName),
        };
        ActivitySource.AddActivityListener(listener);

        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "a",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "from-a"))))
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", GraphConstants.End)
            .Compile(checkpointer);

        var terminal = await graph.InvokeAsync(
            [new ChannelWrite("messages", "seed")],
            new RunOptions { ThreadId = "otel-act-1", StreamMode = StreamMode.Updates });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        started.ShouldContain(VolutaDiagnostics.SuperstepActivityName);
        started.ShouldContain(VolutaDiagnostics.NodeExecuteActivityName);
        started.ShouldContain(VolutaDiagnostics.CheckpointPutActivityName);
    }

    [Fact(DisplayName = "Given MeterListener, when interrupt node runs, then interrupt counter records")]
    public async Task RecordInterruptMetric()
    {
        var box = new MeasurementBox();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == VolutaDiagnostics.SourceName
                    && instrument.Name == VolutaDiagnostics.InterruptCountMetricName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        meterListener.SetMeasurementEventCallback<long>(
            (instrument, measurement, _, _) =>
            {
                if (instrument.Name == VolutaDiagnostics.InterruptCountMetricName)
                {
                    box.Add(measurement);
                }
            });
        meterListener.Start();

        var checkpointer = new InMemoryCheckpointer();
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "gate",
                static (context, _) => context.ResumePayload is null
                    ? Task.FromResult<NodeResult>(NodeResult.Interrupt("approve?"))
                    : Task.FromResult<NodeResult>(
                        NodeResult.Continue(new ChannelWrite("messages", "ok"))))
            .AddEdge(GraphConstants.Start, "gate")
            .AddEdge("gate", GraphConstants.End)
            .Compile(checkpointer);

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "otel-int-1", StreamMode = StreamMode.Events });

        terminal.Kind.ShouldBe(StreamEventKind.Interrupt);
        box.Value.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Given MeterListener, when checkpoint put, then put counter records")]
    public async Task RecordCheckpointPutMetric()
    {
        var box = new MeasurementBox();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == VolutaDiagnostics.SourceName
                    && instrument.Name == VolutaDiagnostics.CheckpointPutCountMetricName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        meterListener.SetMeasurementEventCallback<long>(
            (instrument, measurement, _, _) =>
            {
                if (instrument.Name == VolutaDiagnostics.CheckpointPutCountMetricName)
                {
                    box.Add(measurement);
                }
            });
        meterListener.Start();

        var checkpointer = new InMemoryCheckpointer();
        await checkpointer.PutAsync(
            new CheckpointSnapshot
            {
                ThreadId = "otel-cp-1",
                Step = 0,
                Status = GraphRunStatus.Running,
                ChannelValues = new Dictionary<string, object?>(),
                ChannelVersions = new Dictionary<string, long>(),
                VersionsSeen = new Dictionary<string, IReadOnlyDictionary<string, long>>(),
            });

        box.Value.ShouldBeGreaterThan(0);
    }

    private sealed class MeasurementBox
    {
        private long value;

        public long Value => Interlocked.Read(ref value);

        public void Add(long delta)
        {
            Interlocked.Add(ref value, delta);
        }
    }
}
