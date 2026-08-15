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
    ///     Terminal failure: node exception, concurrent LastValue write, out-of-steps, or other fault.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <strong>Checkpoint policy (last-good payload):</strong> the runtime writes a
    ///         <see cref="Failed" /> snapshot at the failing superstep. Channel values on that
    ///         snapshot are the last successfully applied superstep (incomplete writes are not
    ///         merged). Prior successful steps remain in history when the provider supports list.
    ///     </para>
    ///     <para>
    ///         <see cref="Voluta.Abstractions.Checkpoint.ICheckpointer.GetAsync" /> returns the
    ///         latest step (often this Failed marker). Resume-as-interrupt is rejected; hosts
    ///         re-invoke or rebuild from last-good channel values / list history.
    ///     </para>
    /// </remarks>
    Failed = 3,

    /// <summary>
    ///     Terminal cancellation; not an HITL interrupt and not resumeable as interrupt.
    /// </summary>
    /// <remarks>
    ///     Same last-good payload policy as <see cref="Failed" />: Cancelled is written at the
    ///     aborting superstep without overwriting an earlier successful step's document.
    /// </remarks>
    Cancelled = 4
}
