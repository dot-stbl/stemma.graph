using Microsoft.EntityFrameworkCore;

namespace Voluta.Checkpoints.EntityFrameworkCore;

/// <summary>
///     Contract a host <see cref="DbContext" /> implements to store Voluta checkpoints.
///     Apply model mapping with <see cref="VolutaCheckpointModelExtensions.ApplyVolutaCheckpointModel" />.
/// </summary>
public interface IVolutaCheckpointDbContext
{
    /// <summary>Checkpoint rows keyed by (thread_id, step).</summary>
    public DbSet<CheckpointRecord> Checkpoints { get; }
}
