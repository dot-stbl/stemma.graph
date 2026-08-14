using Voluta.Abstractions.Streaming;

namespace Voluta.Samples.WorkerHost;

/// <summary>
///     How a worker turn ended (park / complete / fail / cancel).
/// </summary>
public enum GraphThreadDisposition
{
    /// <summary>HITL interrupt — checkpoint holds the thread; wait for a resume wake.</summary>
    Parked,

    /// <summary>Terminal success.</summary>
    Completed,

    /// <summary>Terminal failure — last-good checkpoint remains; do not HITL-resume.</summary>
    Failed,

    /// <summary>Cooperative cancel of this turn (host shutdown or token).</summary>
    Cancelled,
}

/// <summary>
///     Result of one wake → invoke/resume cycle.
/// </summary>
public sealed class GraphThreadOutcome
{
    /// <summary>
    ///     Thread that was processed.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    ///     Worker disposition after the turn.
    /// </summary>
    public required GraphThreadDisposition Disposition { get; init; }

    /// <summary>
    ///     Terminal stream event when available.
    /// </summary>
    public StreamEvent? Terminal { get; init; }

    /// <summary>
    ///     Fault when <see cref="Disposition" /> is <see cref="GraphThreadDisposition.Failed" />.
    /// </summary>
    public Exception? Exception { get; init; }
}
