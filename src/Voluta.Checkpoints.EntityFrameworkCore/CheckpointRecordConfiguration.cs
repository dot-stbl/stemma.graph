using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Voluta.Checkpoints.EntityFrameworkCore;

/// <summary>
///     Fluent configuration for <see cref="CheckpointRecord" />.
/// </summary>
public sealed class CheckpointRecordConfiguration : IEntityTypeConfiguration<CheckpointRecord>
{
    /// <summary>Default table name for checkpoint rows.</summary>
    public const string TableName = "voluta_checkpoints";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CheckpointRecord> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(static record => new { record.ThreadId, record.Step });

        builder.Property(static record => record.ThreadId)
            .HasColumnName("thread_id")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(static record => record.Step)
            .HasColumnName("step")
            .IsRequired();

        builder.Property(static record => record.Status)
            .HasColumnName("status")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(static record => record.PayloadJson)
            .HasColumnName("payload_json")
            .IsRequired();

        builder.HasIndex(static record => record.ThreadId)
            .HasDatabaseName("ix_voluta_checkpoints_thread_id");
    }
}
