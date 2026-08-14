using System.Collections.Concurrent;
using Voluta.Abstractions.Checkpoint;
namespace Voluta.Checkpoint;

/// <summary>
///     Process-local C-shape checkpointer for tests and single-process samples.
/// </summary>
/// <remarks>
///     No JSON wire — values are held by reference (cloned on Get/List). Unlike File/EF/S3,
///     InMemory does <strong>not</strong> enforce the durable wire-format v1 value allow-list;
///     any CLR object may be stored within a single process.
/// </remarks>
public sealed class InMemoryCheckpointer : ICheckpointer
{
    private readonly ConcurrentDictionary<string, List<CheckpointSnapshot>> history =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    /// <remarks>
    ///     Ownership: engine <see cref="CheckpointSnapshot" /> instances are treated as exclusive
    ///     after Put — maps are not re-cloned on store. Isolation is enforced on Get/List by cloning.
    /// </remarks>
    public Task PutAsync(CheckpointSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var list = history.GetOrAdd(snapshot.ThreadId, static _ => []);
        lock (list)
        {
            // Store without deep re-clone: RunEngineSnapshots.Build already allocated fresh maps.
            list.Add(snapshot);
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
            var ordered = new List<CheckpointSnapshot>(list.Count);
            foreach (var snapshot in list.OrderBy(static item => item.Step))
            {
                ordered.Add(InMemoryCheckpointClone.Clone(snapshot));
            }

            return Task.FromResult<IReadOnlyList<CheckpointSnapshot>>(ordered);
        }
    }
}

/// <summary>
///     Deep-enough clone of C-shape snapshots so callers cannot mutate stored history.
/// </summary>
file static class InMemoryCheckpointClone
{
    public static CheckpointSnapshot Clone(CheckpointSnapshot snapshot)
    {
        var channelValues = new Dictionary<string, object?>(
            snapshot.ChannelValues.Count,
            StringComparer.Ordinal);
        foreach (var (key, value) in snapshot.ChannelValues)
        {
            channelValues[key] = value;
        }

        var channelVersions = new Dictionary<string, long>(
            snapshot.ChannelVersions.Count,
            StringComparer.Ordinal);
        foreach (var (key, value) in snapshot.ChannelVersions)
        {
            channelVersions[key] = value;
        }

        var versionsSeen = new Dictionary<string, IReadOnlyDictionary<string, long>>(
            snapshot.VersionsSeen.Count,
            StringComparer.Ordinal);
        foreach (var (nodeName, map) in snapshot.VersionsSeen)
        {
            versionsSeen[nodeName] = new Dictionary<string, long>(map, StringComparer.Ordinal);
        }

        return new CheckpointSnapshot
        {
            FormatVersion = snapshot.FormatVersion,
            ThreadId = snapshot.ThreadId,
            Step = snapshot.Step,
            Status = snapshot.Status,
            ChannelValues = channelValues,
            ChannelVersions = channelVersions,
            VersionsSeen = versionsSeen,
            PendingWrites = [.. snapshot.PendingWrites],
            PendingSends = [.. snapshot.PendingSends],
            LastNode = snapshot.LastNode,
            NextNodes = [.. snapshot.NextNodes],
            InterruptPayload = snapshot.InterruptPayload
        };
    }
}
