namespace Voluta.Abstractions.Runtime;

/// <summary>
///     Lifecycle status of a graph run / thread checkpoint.
/// </summary>
public enum GraphRunStatus
{
    /// <summary>
    ///     Supersteps may still be scheduled.
    /// </summary>
    Running = 0,

    /// <summary>
    ///     Paused for human-in-the-loop; resume with <see cref="Command" />.
    /// </summary>
    Interrupted = 1,

    /// <summary>
    ///     Terminal success: no ready tasks remain.
    /// </summary>
    Done = 2,

    /// <summary>
    ///     Terminal failure: node exception, concurrent LastValue write, or other fault.
    /// </summary>
    Failed = 3,

    /// <summary>
    ///     Terminal cancellation; not an HITL interrupt and not resumeable as interrupt.
    /// </summary>
    Cancelled = 4
}
