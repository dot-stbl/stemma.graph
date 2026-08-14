using Microsoft.EntityFrameworkCore;
using Voluta.Abstractions.Checkpoint;

namespace Voluta.Checkpoints.EntityFrameworkCore;

/// <summary>
///     EF Core checkpointer over any <typeparamref name="TContext" /> that implements
///     <see cref="IVolutaCheckpointDbContext" />. Snapshot JSON conversion lives in the
///     EF model (internal entity configuration), not here.
/// </summary>
/// <typeparam name="TContext">Host DbContext type.</typeparam>
/// <remarks>
///     Host registration: <c>v.Checkpoints.UseEntityFrameworkCore&lt;TContext&gt;()</c>.
///     Direct construction is internal for conformance / unit tests only.
/// </remarks>
public sealed class EntityFrameworkCoreCheckpointer<TContext> : ICheckpointer
    where TContext : DbContext, IVolutaCheckpointDbContext
{
    private readonly IDbContextFactory<TContext> factory;

    /// <summary>
    ///     Creates an EF Core checkpointer over <paramref name="factory" />.
    /// </summary>
    /// <param name="factory">Factory for host DbContext instances.</param>
    internal EntityFrameworkCoreCheckpointer(IDbContextFactory<TContext> factory)
    {
        this.factory = factory;
    }

    /// <inheritdoc />
    public async Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            var existing = await db.Checkpoints.FindAsync(
                [snapshot.ThreadId, snapshot.Step],
                cancellationToken);

            if (existing is null)
            {
                db.Checkpoints.Add(new CheckpointRecord
                {
                    ThreadId = snapshot.ThreadId,
                    Step = snapshot.Step,
                    Status = snapshot.Status,
                    Snapshot = snapshot,
                });
            }
            else
            {
                existing.Status = snapshot.Status;
                existing.Snapshot = snapshot;
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                "checkpoint.put_failed",
                $"Failed to put checkpoint for thread '{snapshot.ThreadId}' step {snapshot.Step}.",
                exception);
        }
    }

    /// <inheritdoc />
    public async Task<CheckpointSnapshot?> GetAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            var record = await db.Checkpoints
                .AsNoTracking()
                .Where(row => row.ThreadId == threadId)
                .OrderByDescending(row => row.Step)
                .FirstOrDefaultAsync(cancellationToken);

            return record?.Snapshot;
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                "checkpoint.get_failed",
                $"Failed to get checkpoint for thread '{threadId}'.",
                exception);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CheckpointSnapshot>> ListAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            return await db.Checkpoints
                .AsNoTracking()
                .Where(row => row.ThreadId == threadId)
                .OrderBy(row => row.Step)
                .Select(row => row.Snapshot)
                .ToListAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException
                                          and not CheckpointStoreException)
        {
            throw new CheckpointStoreException(
                "checkpoint.list_failed",
                $"Failed to list checkpoints for thread '{threadId}'.",
                exception);
        }
    }
}

/// <summary>
///     Non-generic façade over dedicated <see cref="VolutaCheckpointDbContext" />.
/// </summary>
/// <remarks>
///     Host registration: <c>v.Checkpoints.UseEntityFrameworkCore()</c>.
///     Direct construction is internal for conformance / unit tests only.
/// </remarks>
public sealed class EntityFrameworkCoreCheckpointer : ICheckpointer
{
    private readonly EntityFrameworkCoreCheckpointer<VolutaCheckpointDbContext> inner;

    /// <summary>
    ///     Creates a checkpointer over dedicated <see cref="VolutaCheckpointDbContext" />.
    /// </summary>
    /// <param name="factory">Factory for <see cref="VolutaCheckpointDbContext" />.</param>
    internal EntityFrameworkCoreCheckpointer(IDbContextFactory<VolutaCheckpointDbContext> factory)
    {
        inner = new EntityFrameworkCoreCheckpointer<VolutaCheckpointDbContext>(factory);
    }

    /// <inheritdoc />
    public Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        return inner.PutAsync(snapshot, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CheckpointSnapshot?> GetAsync(string threadId, CancellationToken cancellationToken = default)
    {
        return inner.GetAsync(threadId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CheckpointSnapshot>> ListAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        return inner.ListAsync(threadId, cancellationToken);
    }
}
