using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Voluta.Abstractions.Checkpoint;
using Voluta.Checkpoints.EntityFrameworkCore.Wire;

namespace Voluta.Checkpoints.EntityFrameworkCore;

/// <summary>
///     EF Core checkpointer over any <typeparamref name="TContext" /> that implements
///     <see cref="IVolutaCheckpointDbContext" />. Prefer <see cref="IDbContextFactory{TContext}" />.
/// </summary>
/// <typeparam name="TContext">Host DbContext type.</typeparam>
/// <remarks>
///     Values use System.Text.Json; prefer JSON-friendly types (strings, numbers, lists of primitives).
/// </remarks>
public sealed class EntityFrameworkCoreCheckpointer<TContext>(IDbContextFactory<TContext> factory)
    : ICheckpointer
    where TContext : DbContext, IVolutaCheckpointDbContext
{
    /// <inheritdoc />
    public async Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var document = EfCheckpointDocument.FromSnapshot(snapshot);
        var json = JsonSerializer.Serialize(document, JsonSerializerOptions.Web);

        var existing = await db.Checkpoints.FindAsync(
            [snapshot.ThreadId, snapshot.Step],
            cancellationToken);

        if (existing is null)
        {
            db.Checkpoints.Add(new CheckpointRecord
            {
                ThreadId = snapshot.ThreadId,
                Step = snapshot.Step,
                Status = snapshot.Status.ToString(),
                PayloadJson = json,
            });
        }
        else
        {
            existing.Status = snapshot.Status.ToString();
            existing.PayloadJson = json;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CheckpointSnapshot?> GetAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var record = await db.Checkpoints
            .AsNoTracking()
            .Where(row => row.ThreadId == threadId)
            .OrderByDescending(row => row.Step)
            .FirstOrDefaultAsync(cancellationToken);

        return record is null ? null : EfCheckpointPayload.Deserialize(record.PayloadJson);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CheckpointSnapshot>> ListAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var records = await db.Checkpoints
            .AsNoTracking()
            .Where(row => row.ThreadId == threadId)
            .OrderBy(row => row.Step)
            .ToListAsync(cancellationToken);

        var list = new List<CheckpointSnapshot>(records.Count);
        foreach (var record in records)
        {
            var snapshot = EfCheckpointPayload.Deserialize(record.PayloadJson);
            if (snapshot is not null)
            {
                list.Add(snapshot);
            }
        }

        return list;
    }
}

/// <summary>
///     Non-generic façade over dedicated <see cref="VolutaCheckpointDbContext" />
///     (wraps <c>EntityFrameworkCoreCheckpointer&lt;VolutaCheckpointDbContext&gt;</c>).
/// </summary>
public sealed class EntityFrameworkCoreCheckpointer(IDbContextFactory<VolutaCheckpointDbContext> factory)
    : ICheckpointer
{
    private readonly EntityFrameworkCoreCheckpointer<VolutaCheckpointDbContext> inner = new(factory);

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

/// <summary>
///     Deserialize helpers for checkpoint payload JSON.
/// </summary>
file static class EfCheckpointPayload
{
    public static CheckpointSnapshot? Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<EfCheckpointDocument>(json, JsonSerializerOptions.Web);
        return document?.ToSnapshot();
    }
}
