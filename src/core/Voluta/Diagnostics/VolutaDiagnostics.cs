using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Voluta.Diagnostics;

/// <summary>
///     BCL <see cref="ActivitySource" /> and <see cref="Meter" /> for the Voluta runtime.
///     Always available; no OpenTelemetry SDK dependency. Hosts opt in via
///     <c>Voluta.OpenTelemetry</c> (<c>AddVolutaInstrumentation</c>).
/// </summary>
public static class VolutaDiagnostics
{
    /// <summary>
    ///     Activity source / meter name (matches assembly name).
    /// </summary>
    public const string SourceName = "Voluta";

    /// <summary>
    ///     Superstep activity name.
    /// </summary>
    public const string SuperstepActivityName = "voluta.superstep";

    /// <summary>
    ///     Per-node execution activity name.
    /// </summary>
    public const string NodeExecuteActivityName = "voluta.node.execute";

    /// <summary>
    ///     Checkpoint put activity name.
    /// </summary>
    public const string CheckpointPutActivityName = "voluta.checkpoint.put";

    /// <summary>
    ///     Checkpoint get activity name.
    /// </summary>
    public const string CheckpointGetActivityName = "voluta.checkpoint.get";

    /// <summary>
    ///     Checkpoint list activity name.
    /// </summary>
    public const string CheckpointListActivityName = "voluta.checkpoint.list";

    /// <summary>
    ///     Metric: superstep wall duration (unit: ms).
    /// </summary>
    public const string SuperstepDurationMetricName = "voluta.superstep.duration";

    /// <summary>
    ///     Metric: node execution wall duration (unit: ms).
    /// </summary>
    public const string NodeDurationMetricName = "voluta.node.duration";

    /// <summary>
    ///     Metric: interrupt count when a node returns <c>NodeResult.Interrupt</c>.
    /// </summary>
    public const string InterruptCountMetricName = "voluta.interrupt.count";

    /// <summary>
    ///     Metric: checkpoint put operations.
    /// </summary>
    public const string CheckpointPutCountMetricName = "voluta.checkpoint.put.count";

    /// <summary>
    ///     Metric: checkpoint get operations.
    /// </summary>
    public const string CheckpointGetCountMetricName = "voluta.checkpoint.get.count";

    /// <summary>
    ///     Metric: checkpoint list operations.
    /// </summary>
    public const string CheckpointListCountMetricName = "voluta.checkpoint.list.count";

    /// <summary>
    ///     Metric: live stream events dropped under backpressure.
    /// </summary>
    public const string StreamDroppedMetricName = "voluta.stream.dropped";

    /// <summary>
    ///     Tag key for graph node name (bounded by topology).
    /// </summary>
    public const string TagNodeName = "node.name";

    /// <summary>
    ///     Tag key for run / checkpoint status (enum label).
    /// </summary>
    public const string TagRunStatus = "run.status";

    /// <summary>
    ///     Tag key for exception type name on failed operations.
    /// </summary>
    public const string TagErrorType = "error.type";

    /// <summary>
    ///     Tag key for checkpointer provider name (e.g. <c>inmemory</c>).
    /// </summary>
    public const string TagProviderName = "provider.name";

    /// <summary>
    ///     Tag key for stream event kind on drop metrics (<c>custom</c> / <c>messages</c>).
    /// </summary>
    public const string TagStreamKind = "stream.kind";

    /// <summary>
    ///     Shared activity source for the Voluta assembly.
    /// </summary>
    public static ActivitySource ActivitySource { get; } = new(SourceName);

    /// <summary>
    ///     Shared meter for the Voluta assembly.
    /// </summary>
    public static Meter Meter { get; } = new(SourceName);

    /// <summary>
    ///     Superstep duration histogram (milliseconds).
    /// </summary>
    public static Histogram<double> SuperstepDuration { get; } =
        Meter.CreateHistogram<double>(SuperstepDurationMetricName, unit: "ms");

    /// <summary>
    ///     Node execution duration histogram (milliseconds).
    /// </summary>
    public static Histogram<double> NodeDuration { get; } =
        Meter.CreateHistogram<double>(NodeDurationMetricName, unit: "ms");

    /// <summary>
    ///     Count of HITL interrupts.
    /// </summary>
    public static Counter<long> InterruptCount { get; } =
        Meter.CreateCounter<long>(InterruptCountMetricName, unit: "{interrupt}");

    /// <summary>
    ///     Count of checkpoint put operations.
    /// </summary>
    public static Counter<long> CheckpointPutCount { get; } =
        Meter.CreateCounter<long>(CheckpointPutCountMetricName, unit: "{operation}");

    /// <summary>
    ///     Count of checkpoint get operations.
    /// </summary>
    public static Counter<long> CheckpointGetCount { get; } =
        Meter.CreateCounter<long>(CheckpointGetCountMetricName, unit: "{operation}");

    /// <summary>
    ///     Count of checkpoint list operations.
    /// </summary>
    public static Counter<long> CheckpointListCount { get; } =
        Meter.CreateCounter<long>(CheckpointListCountMetricName, unit: "{operation}");

    /// <summary>
    ///     Count of dropped live stream events (bounded channel overflow).
    /// </summary>
    public static Counter<long> StreamDropped { get; } =
        Meter.CreateCounter<long>(StreamDroppedMetricName, unit: "{event}");
}
