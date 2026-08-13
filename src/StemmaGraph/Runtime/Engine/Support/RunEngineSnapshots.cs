using StemmaGraph.Abstractions.Checkpoint;
using StemmaGraph.Abstractions.Runtime;

namespace StemmaGraph.Runtime.Engine.Support;

/// <summary>
///     Checkpoint snapshot builders for the run engine.
/// </summary>
internal static class RunEngineSnapshots
{
    public static CheckpointSnapshot Build(
        string threadId,
        long step,
        GraphRunStatus status,
        ChannelStore store,
        string? lastNode,
        IReadOnlyList<string> nextNodes,
        IReadOnlyList<PendingSend> pendingSends,
        object? interruptPayload)
    {
        return new CheckpointSnapshot
        {
            ThreadId = threadId,
            Step = step,
            Status = status,
            ChannelValues = store.SnapshotValues(),
            ChannelVersions = new Dictionary<string, long>(store.Versions, StringComparer.Ordinal),
            VersionsSeen = store.VersionsSeen,
            PendingWrites = [],
            PendingSends = [.. pendingSends],
            LastNode = lastNode,
            NextNodes = [.. nextNodes],
            InterruptPayload = interruptPayload
        };
    }
}
