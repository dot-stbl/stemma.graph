using StemmaGraph.Abstractions.Checkpoint;
using System.Collections.Concurrent;
namespace StemmaGraph.Checkpoint;

/// <summary>
///     Process-local C-shape checkpointer for tests and single-process samples.
/// </summary>
public sealed class InMemoryCheckpointer : ICheckpointer
{
    private readonly ConcurrentDictionary<string, List<CheckpointSnapshot>> history =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var list = history.GetOrAdd(snapshot.ThreadId, static _ => []);
        lock (list)
        {
            list.Add(InMemoryCheckpointClone.Clone(snapshot));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<CheckpointSnapshot?> GetAsync(string threadId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!history.TryGetValue(threadId, out var list))
        {
            return Task.FromResult<CheckpointSnapshot?>(null);
        }

        lock (list)
        {
            return list.Count == 0
                ? Task.FromResult<CheckpointSnapshot?>(null)
                : Task.FromResult<CheckpointSnapshot?>(InMemoryCheckpointClone.Clone(list[^1]));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CheckpointSnapshot>> ListAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!history.TryGetValue(threadId, out var list))
        {
            return Task.FromResult<IReadOnlyList<CheckpointSnapshot>>([]);
        }

        lock (list)
        {
            var ordered = list
                .OrderBy(static snapshot => snapshot.Step)
                .Select(InMemoryCheckpointClone.Clone)
                .ToList();
            return Task.FromResult<IReadOnlyList<CheckpointSnapshot>>(ordered);
        }
    }
}

/// <summary>
///     Deep-enough clone of C-shape snapshots for InMemory isolation.
/// </summary>
file static class InMemoryCheckpointClone
{
    public static CheckpointSnapshot Clone(CheckpointSnapshot snapshot)
    {
        return new CheckpointSnapshot
        {
            FormatVersion = snapshot.FormatVersion,
            ThreadId = snapshot.ThreadId,
            Step = snapshot.Step,
            Status = snapshot.Status,
            ChannelValues = new Dictionary<string, object?>(snapshot.ChannelValues, StringComparer.Ordinal),
            ChannelVersions = new Dictionary<string, long>(snapshot.ChannelVersions, StringComparer.Ordinal),
            VersionsSeen = snapshot.VersionsSeen.ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlyDictionary<string, long>)new Dictionary<string, long>(
                    pair.Value,
                    StringComparer.Ordinal),
                StringComparer.Ordinal),
            PendingWrites = [.. snapshot.PendingWrites],
            PendingSends = [.. snapshot.PendingSends],
            LastNode = snapshot.LastNode,
            NextNodes = [.. snapshot.NextNodes],
            InterruptPayload = snapshot.InterruptPayload
        };
    }
}
