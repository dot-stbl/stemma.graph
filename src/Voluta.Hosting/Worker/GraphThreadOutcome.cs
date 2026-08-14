using Voluta.Abstractions.Streaming;

namespace Voluta.Hosting.Worker;

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
