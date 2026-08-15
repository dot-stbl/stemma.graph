using Microsoft.EntityFrameworkCore;

namespace Voluta.Checkpoints.EntityFrameworkCore;

/// <summary>
///     Optional dedicated EF context for Voluta checkpoints only.
///     Hosts that already have an app DbContext should implement
///     <see cref="IVolutaCheckpointDbContext" /> and call
///     <see cref="VolutaCheckpointModelExtensions.ApplyVolutaCheckpointModel" /> instead.
/// </summary>
/// <remarks>
///     Schema ownership: consumers apply migrations or call
///     <c>Database.EnsureCreated()</c> in tests. This package ships no design-time factory.
/// </remarks>
public sealed class VolutaCheckpointDbContext(DbContextOptions<VolutaCheckpointDbContext> options)
    : DbContext(options), IVolutaCheckpointDbContext
{
    /// <inheritdoc />
    public DbSet<CheckpointRecord> Checkpoints => Set<CheckpointRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyVolutaCheckpointModel();
        base.OnModelCreating(modelBuilder);
    }
}
