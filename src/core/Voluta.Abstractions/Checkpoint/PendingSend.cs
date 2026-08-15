namespace Voluta.Abstractions.Checkpoint;

/// <summary>
///     A PUSH/Send task scheduled for a future superstep (checkpoint extension for Send).
/// </summary>
public sealed class PendingSend
{
    /// <summary>
    ///     Target node name.
    /// </summary>
    public required string NodeName { get; init; }

    /// <summary>
    ///     Task-local payload (may be null).
    /// </summary>
    public object? Payload { get; init; }

    /// <summary>
    ///     Stable task id for diagnostics (defaults empty).
    /// </summary>
    public string TaskId { get; init; } = "";
}
