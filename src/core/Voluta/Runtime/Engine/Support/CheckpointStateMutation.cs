using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Topology;
using Voluta.Exceptions.Run;
using Voluta.Graph;

namespace Voluta.Runtime.Engine.Support;

/// <summary>
///     Host-facing checkpoint mutations: apply channel writes and fork threads.
///     Uses <see cref="ChannelStore" /> so LastValue / Append reducers stay correct.
/// </summary>
internal static class CheckpointStateMutation
{
    /// <summary>
    ///     Loads the latest checkpoint, applies writes via channel reducers, puts a new step.
    /// </summary>
    public static async Task<ThreadSnapshot> UpdateStateAsync(
        GraphTopology topology,
        ICheckpointer checkpointer,
        string threadId,
        IEnumerable<ChannelWrite> writes,
        CancellationToken cancellationToken)
    {
        var latest = await checkpointer.GetAsync(threadId, cancellationToken)
                     ?? throw new GraphThreadNotFoundException(
                         $"No checkpoint found for thread '{threadId}'.");

        var writeList = writes as IList<ChannelWrite> ?? writes.ToList();
        var store = new ChannelStore(topology.Channels);
        store.Restore(latest.ChannelValues, latest.ChannelVersions, latest.VersionsSeen);
        if (writeList.Count > 0)
        {
            store.ApplyInputWrites(writeList);
        }

        // Host edits append a new history step so List remains ordered and Get returns the edit.
        var nextStep = latest.Step + 1;
        var status = NormalizeStatusForHostEdit(latest.Status);
        var nextNodes = status == GraphRunStatus.Running
            ? latest.NextNodes
            : Array.Empty<string>();
        var interruptPayload = status == GraphRunStatus.Interrupted
            ? latest.InterruptPayload
            : null;
        var pendingSends = status == GraphRunStatus.Running
            ? latest.PendingSends
            : Array.Empty<PendingSend>();

        var snapshot = RunEngineSnapshots.Build(
            threadId,
            nextStep,
            status,
            store,
            latest.LastNode,
            nextNodes,
            pendingSends,
            interruptPayload);

        await checkpointer.PutAsync(snapshot, cancellationToken);
        return ThreadSnapshotMapping.FromSnapshot(snapshot);
    }

    /// <summary>
    ///     Copies the checkpoint at <paramref name="step" /> onto <paramref name="newThreadId" />
    ///     at the same step index (history root for the new thread).
    /// </summary>
    public static async Task<ThreadSnapshot> ForkAsync(
        ICheckpointer checkpointer,
        string sourceThreadId,
        long step,
        string newThreadId,
        CancellationToken cancellationToken)
    {
        var history = await checkpointer.ListAsync(sourceThreadId, cancellationToken);
        if (history.Count == 0)
        {
            throw new GraphThreadNotFoundException(
                $"No checkpoint found for thread '{sourceThreadId}'.");
        }

        CheckpointSnapshot? source = null;
        foreach (var item in history)
        {
            if (item.Step == step)
            {
                source = item;
                break;
            }
        }

        if (source is null)
        {
            throw new GraphStepNotFoundException(
                $"No checkpoint at step {step} for thread '{sourceThreadId}'.");
        }

        var forked = new CheckpointSnapshot
        {
            FormatVersion = source.FormatVersion,
            ThreadId = newThreadId,
            Step = source.Step,
            Status = source.Status,
            ChannelValues = CloneValues(source.ChannelValues),
            ChannelVersions = CloneVersions(source.ChannelVersions),
            VersionsSeen = CloneVersionsSeen(source.VersionsSeen),
            PendingWrites = [.. source.PendingWrites],
            PendingSends = [.. source.PendingSends],
            LastNode = source.LastNode,
            NextNodes = [.. source.NextNodes],
            InterruptPayload = source.InterruptPayload,
        };

        await checkpointer.PutAsync(forked, cancellationToken);
        return ThreadSnapshotMapping.FromSnapshot(forked);
    }

    /// <summary>
    ///     Terminal Failed/Cancelled become Running so Continue can re-drive NextNodes;
    ///     Done stays Done (ops may still patch values without re-execution).
    /// </summary>
    public static GraphRunStatus NormalizeStatusForHostEdit(GraphRunStatus status)
    {
        return status is GraphRunStatus.Failed or GraphRunStatus.Cancelled
            ? GraphRunStatus.Running
            : status;
    }

    public static Dictionary<string, object?> CloneValues(IReadOnlyDictionary<string, object?> values)
    {
        var clone = new Dictionary<string, object?>(values.Count, StringComparer.Ordinal);
        foreach (var (key, value) in values)
        {
            clone[key] = value;
        }

        return clone;
    }

    public static Dictionary<string, long> CloneVersions(IReadOnlyDictionary<string, long> versions)
    {
        var clone = new Dictionary<string, long>(versions.Count, StringComparer.Ordinal);
        foreach (var (key, value) in versions)
        {
            clone[key] = value;
        }

        return clone;
    }

    public static Dictionary<string, IReadOnlyDictionary<string, long>> CloneVersionsSeen(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, long>> seen)
    {
        var clone = new Dictionary<string, IReadOnlyDictionary<string, long>>(
            seen.Count,
            StringComparer.Ordinal);
        foreach (var (nodeName, map) in seen)
        {
            clone[nodeName] = new Dictionary<string, long>(map, StringComparer.Ordinal);
        }

        return clone;
    }
}
