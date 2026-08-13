namespace Voluta.Checkpoints.EntityFrameworkCore;

/// <summary>
///     EF entity for one C-shape checkpoint row (composite key thread + step).
/// </summary>
public sealed class CheckpointRecord
{
    /// <summary>Thread identifier (part of composite key).</summary>
    public required string ThreadId { get; set; }

    /// <summary>Superstep number (part of composite key).</summary>
    public long Step { get; set; }

    /// <summary>Graph run status name (e.g. Running, Done, Interrupted).</summary>
    public required string Status { get; set; }

    /// <summary>Full C-shape JSON document (same wire shape as the file provider).</summary>
    public required string PayloadJson { get; set; }
}
