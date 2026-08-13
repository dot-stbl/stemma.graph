using System.Collections.Concurrent;
using StemmaGraph.Abstractions.Checkpoint;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Abstractions.Streaming;
using StemmaGraph.Abstractions.Topology;
using StemmaGraph.Graph;

namespace StemmaGraph.UI;

/// <summary>
///     In-process session store for the ops UI (one host → one graph + checkpointer).
/// </summary>
/// <remarks>
///     Creates a session bound to a compiled graph and its checkpointer.
/// </remarks>
/// <param name="graph">Runnable graph.</param>
/// <param name="checkpointer">Same checkpointer used at compile time.</param>
public sealed class StemmaUiSession(CompiledGraph graph, ICheckpointer checkpointer)
{
    private readonly ConcurrentDictionary<string, byte> knownThreads = new(StringComparer.Ordinal);

    /// <summary>
    ///     Compiled graph.
    /// </summary>
    public CompiledGraph Graph { get; } = graph;

    /// <summary>
    ///     Checkpoint store.
    /// </summary>
    public ICheckpointer Checkpointer { get; } = checkpointer;

    /// <summary>
    ///     Topology export for the topology screen.
    /// </summary>
    public GraphDescription Topology => Graph.Describe();

    /// <summary>
    ///     Remembers a thread id after an invoke/stream so the HITL queue can list it.
    /// </summary>
    /// <param name="threadId">Thread id.</param>
    public void TrackThread(string threadId)
    {
        knownThreads[threadId] = 0;
    }

    /// <summary>
    ///     Lists tracked thread ids that currently have an interrupted checkpoint.
    /// </summary>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Interrupted thread summaries.</returns>
    public async Task<IReadOnlyList<HitlThreadSummary>> ListInterruptedAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<HitlThreadSummary>();
        foreach (var threadId in knownThreads.Keys.OrderBy(static id => id, StringComparer.Ordinal))
        {
            var snapshot = await Checkpointer.GetAsync(threadId, cancellationToken);
            if (snapshot?.Status == GraphRunStatus.Interrupted)
            {
                result.Add(
                    new HitlThreadSummary
                    {
                        ThreadId = threadId,
                        Step = snapshot.Step,
                        InterruptPayload = snapshot.InterruptPayload?.ToString(),
                        LastNode = snapshot.LastNode,
                    });
            }
        }

        return result;
    }

    /// <summary>
    ///     Loads the latest checkpoint for a thread.
    /// </summary>
    public Task<CheckpointSnapshot?> GetCheckpointAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        return Checkpointer.GetAsync(threadId, cancellationToken);
    }

    /// <summary>
    ///     Resumes an interrupted thread with a command.
    /// </summary>
    public async Task<StreamEvent> ResumeAsync(
        string threadId,
        Command command,
        CancellationToken cancellationToken = default)
    {
        TrackThread(threadId);
        return await Graph.ResumeInvokeAsync(threadId, command, cancellationToken);
    }
}

/// <summary>
///     HITL queue row.
/// </summary>
public sealed class HitlThreadSummary
{
    /// <summary>
    ///     Thread id.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    ///     Superstep of the interrupt.
    /// </summary>
    public long Step { get; init; }

    /// <summary>
    ///     Last node name.
    /// </summary>
    public string? LastNode { get; init; }

    /// <summary>
    ///     Interrupt payload string form.
    /// </summary>
    public string? InterruptPayload { get; init; }
}
