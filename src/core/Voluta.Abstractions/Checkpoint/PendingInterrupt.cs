namespace Voluta.Abstractions.Checkpoint;

/// <summary>
///     One HITL interrupt from a parallel ready task (stable across checkpoint/resume).
/// </summary>
public sealed class PendingInterrupt
{
    /// <summary>
    ///     Stable task id (pull tasks use node name; Send uses the scheduled task id).
    /// </summary>
    public required string TaskId { get; init; }

    /// <summary>
    ///     Node that returned the interrupt result.
    /// </summary>
    public required string NodeName { get; init; }

    /// <summary>
    ///     Interrupt payload from <c>NodeResult.Interrupt</c> for host/UI.
    /// </summary>
    public object? Payload { get; init; }

    /// <summary>
    ///     Send/PUSH task payload when this invocation was scheduled via Send (may be null).
    /// </summary>
    public object? TaskPayload { get; init; }
}
