// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Runtime;

namespace StemmaGraph.Checkpoint;

/// <summary>
/// Full C-shape checkpoint: channel values, versions, versions_seen, pending writes,
/// step, status, and optional interrupt payload.
/// </summary>
public sealed class CheckpointSnapshot
{
    /// <summary>
    /// Schema version for checkpoint evolution / conformance.
    /// </summary>
    public int FormatVersion { get; init; } = 1;

    /// <summary>
    /// Thread (conversation / run) identifier isolating mutable state.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    /// Superstep index associated with this snapshot.
    /// </summary>
    public long Step { get; init; }

    /// <summary>
    /// Run status at the time of the snapshot.
    /// </summary>
    public GraphRunStatus Status { get; init; }

    /// <summary>
    /// Channel name → current value.
    /// </summary>
    public IReadOnlyDictionary<string, object?> ChannelValues { get; init; }
        = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Channel name → monotonic version after last successful apply.
    /// </summary>
    public IReadOnlyDictionary<string, long> ChannelVersions { get; init; }
        = new Dictionary<string, long>(StringComparer.Ordinal);

    /// <summary>
    /// Per-node map of channel → last version seen by that node (for ready-set prep).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, long>> VersionsSeen { get; init; }
        = new Dictionary<string, IReadOnlyDictionary<string, long>>(StringComparer.Ordinal);

    /// <summary>
    /// Incomplete-task writes retained for mid-superstep recovery.
    /// </summary>
    public IReadOnlyList<PendingWrite> PendingWrites { get; init; } = [];

    /// <summary>
    /// Last node (or task) that completed before this snapshot, when known.
    /// </summary>
    public string? LastNode { get; init; }

    /// <summary>
    /// Next node names scheduled or expected, when known.
    /// </summary>
    public IReadOnlyList<string> NextNodes { get; init; } = [];

    /// <summary>
    /// HITL interrupt payload when <see cref="Status"/> is <see cref="GraphRunStatus.Interrupted"/>.
    /// </summary>
    public object? InterruptPayload { get; init; }
}
