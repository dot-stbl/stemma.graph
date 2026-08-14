namespace Voluta.UI;

/// <summary>
///     HITL queue row.
/// </summary>
public sealed class HitlThreadSummary
{
    /// <summary>
    ///     Thread id.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    ///     Superstep of the interrupt.
    /// </summary>
    public long Step { get; init; }

    /// <summary>
    ///     Last node name.
    /// </summary>
    public string? LastNode { get; init; }

    /// <summary>
    ///     Interrupt payload string form.
    /// </summary>
    public string? InterruptPayload { get; init; }
}
