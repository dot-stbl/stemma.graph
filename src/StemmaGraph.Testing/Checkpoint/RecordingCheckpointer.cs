using System.Collections.Concurrent;
using StemmaGraph.Abstractions.Checkpoint;
namespace StemmaGraph.Testing.Checkpoint;

/// <summary>
///     <see cref="ICheckpointer" /> decorator that records Put/Get/List calls for assertions.
/// </summary>
public sealed class RecordingCheckpointer(ICheckpointer inner) : ICheckpointer
{
    private readonly ConcurrentQueue<CheckpointGetRecord> gets = new();
    private readonly ConcurrentQueue<CheckpointListRecord> lists = new();
    private readonly ConcurrentQueue<CheckpointSnapshot> puts = new();

    /// <summary>
    ///     Snapshots passed to <see cref="PutAsync" />, in call order.
    /// </summary>
    public IReadOnlyList<CheckpointSnapshot> Puts => [.. puts];

    /// <summary>
    ///     Get calls (thread id + result), in call order.
    /// </summary>
    public IReadOnlyList<CheckpointGetRecord> Gets => [.. gets];

    /// <summary>
    ///     List calls (thread id + result), in call order.
    /// </summary>
    public IReadOnlyList<CheckpointListRecord> Lists => [.. lists];

    /// <inheritdoc />
    public async Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await inner.PutAsync(snapshot, cancellationToken);
        puts.Enqueue(snapshot);
    }

    /// <inheritdoc />
    public async Task<CheckpointSnapshot?> GetAsync(string threadId, CancellationToken cancellationToken = default)
    {
        var result = await inner.GetAsync(threadId, cancellationToken);
        gets.Enqueue(new CheckpointGetRecord(threadId, result));
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CheckpointSnapshot>> ListAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.ListAsync(threadId, cancellationToken);
        lists.Enqueue(new CheckpointListRecord(threadId, result));
        return result;
    }
}

/// <summary>
///     Recorded <see cref="ICheckpointer.GetAsync" /> invocation.
/// </summary>
/// <param name="ThreadId">Thread identifier passed to Get.</param>
/// <param name="Result">Snapshot returned by the inner checkpointer.</param>
public sealed record CheckpointGetRecord(string ThreadId, CheckpointSnapshot? Result);

/// <summary>
///     Recorded <see cref="ICheckpointer.ListAsync" /> invocation.
/// </summary>
/// <param name="ThreadId">Thread identifier passed to List.</param>
/// <param name="Result">Snapshots returned by the inner checkpointer.</param>
public sealed record CheckpointListRecord(string ThreadId, IReadOnlyList<CheckpointSnapshot> Result);
