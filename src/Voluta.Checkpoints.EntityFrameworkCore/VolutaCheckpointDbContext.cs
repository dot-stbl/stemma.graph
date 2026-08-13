using Microsoft.EntityFrameworkCore;

namespace Voluta.Checkpoints.EntityFrameworkCore;

/// <summary>
///     EF Core context for Voluta C-shape checkpoints. Register with any EF provider
///     (Npgsql, SqlServer, SQLite, …) — this package does not depend on a concrete provider.
/// </summary>
/// <remarks>
///     Schema ownership: consumers apply migrations or call
///     <c>Database.EnsureCreated()</c> in tests. This package ships no design-time factory.
/// </remarks>
public sealed class VolutaCheckpointDbContext(DbContextOptions<VolutaCheckpointDbContext> options)
    : DbContext(options)
{
    /// <summary>Checkpoint rows keyed by (thread_id, step).</summary>
    public DbSet<CheckpointRecord> Checkpoints => Set<CheckpointRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CheckpointRecordConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
