namespace Voluta.UI;

/// <summary>
///     Tracked thread row for the ops console thread list.
/// </summary>
public sealed class ThreadSummary
{
    /// <summary>
    ///     Thread id.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    ///     Latest run status name.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    ///     Superstep of the latest checkpoint.
    /// </summary>
    public long Step { get; init; }

    /// <summary>
    ///     Last node name when known.
    /// </summary>
    public string? LastNode { get; init; }

    /// <summary>
    ///     Optional goal channel value (demo graphs).
    /// </summary>
    public string? Goal { get; init; }
}
