using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Runtime;

namespace Voluta.Abstractions.Results;

/// <summary>
///     Successful node completion carrying partial channel writes and optional Send fan-out.
/// </summary>
/// <remarks>
///     Initializes a continue result.
/// </remarks>
/// <param name="writes">Partial channel updates; empty means no channel changes.</param>
/// <param name="sends">Optional PUSH tasks for the next superstep.</param>
public sealed class ContinueNodeResult(
    IReadOnlyList<ChannelWrite> writes,
    IReadOnlyList<Send>? sends = null) : NodeResult
{
    /// <summary>
    ///     Partial channel writes produced by the node.
    /// </summary>
    public IReadOnlyList<ChannelWrite> Writes { get; } = writes;

    /// <summary>
    ///     Dynamic Send tasks to schedule after the barrier.
    /// </summary>
    public IReadOnlyList<Send> Sends { get; } = sends ?? [];
}
