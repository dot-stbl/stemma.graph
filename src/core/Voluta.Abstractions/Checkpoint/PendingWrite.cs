namespace Voluta.Abstractions.Checkpoint;

/// <summary>
///     A write produced by a completed task that has not yet been fully applied
///     (mid-superstep crash recovery).
/// </summary>
public sealed class PendingWrite
{
    /// <summary>
    ///     Task or node identity that produced the write.
    /// </summary>
    public required string TaskId { get; init; }

    /// <summary>
    ///     Channel name the write targets.
    /// </summary>
    public required string ChannelName { get; init; }

    /// <summary>
    ///     Written value (null is an explicit clear when present).
    /// </summary>
    public object? Value { get; init; }
}
