using Microsoft.EntityFrameworkCore;

namespace Voluta.Checkpoints.EntityFrameworkCore;

/// <summary>
///     Model-builder helpers for embedding Voluta checkpoints in a host DbContext.
/// </summary>
public static class VolutaCheckpointModelExtensions
{
    /// <summary>
    ///     Applies the <see cref="CheckpointRecord" /> entity configuration (table
    ///     <c>voluta_checkpoints</c>, composite key, indexes).
    /// </summary>
    /// <param name="modelBuilder">EF model builder from <c>OnModelCreating</c>.</param>
    /// <returns>The same model builder for chaining.</returns>
    public static ModelBuilder ApplyVolutaCheckpointModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CheckpointRecordConfiguration());
        return modelBuilder;
    }
}
