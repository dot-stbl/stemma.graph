using System.Collections.Concurrent;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Abstractions.Topology;
using Voluta.Graph;

namespace Voluta.UI;

/// <summary>
///     In-process session store for the ops UI (one host → one graph + checkpointer).
/// </summary>
/// <remarks>
///     Creates a session bound to a compiled graph and its checkpointer.
/// </remarks>
/// <param name="graph">Runnable graph.</param>
/// <param name="checkpointer">Same checkpointer used at compile time.</param>
public sealed class VolutaUiSession(CompiledGraph graph, ICheckpointer checkpointer)
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
    ///     Lists every tracked thread with its latest checkpoint status (for the inspector list).
    /// </summary>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Thread summaries ordered by id.</returns>
    public async Task<IReadOnlyList<ThreadSummary>> ListThreadsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<ThreadSummary>();
        foreach (var threadId in knownThreads.Keys.OrderBy(static id => id, StringComparer.Ordinal))
        {
            var snapshot = await Checkpointer.GetAsync(threadId, cancellationToken);
            if (snapshot is null)
            {
                result.Add(
                    new ThreadSummary
                    {
                        ThreadId = threadId,
                        Status = "Unknown",
                        Step = 0,
                    });
                continue;
            }

            string? goal = null;
            if (snapshot.ChannelValues.TryGetValue("goal", out var goalValue) && goalValue is not null)
            {
                goal = goalValue.ToString();
            }

            result.Add(
                new ThreadSummary
                {
                    ThreadId = threadId,
                    Status = snapshot.Status.ToString(),
                    Step = snapshot.Step,
                    LastNode = snapshot.LastNode,
                    Goal = goal,
                });
        }

        return result;
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
        foreach (var thread in await ListThreadsAsync(cancellationToken))
        {
            if (thread.Status != GraphRunStatus.Interrupted.ToString())
            {
                continue;
            }

            var snapshot = await Checkpointer.GetAsync(thread.ThreadId, cancellationToken);
            if (snapshot is null)
            {
                continue;
            }

            result.Add(
                new HitlThreadSummary
                {
                    ThreadId = thread.ThreadId,
                    Step = snapshot.Step,
                    InterruptPayload = snapshot.InterruptPayload?.ToString(),
                    LastNode = snapshot.LastNode,
                });
        }

        return result;
    }

    /// <summary>
    ///     Loads the latest checkpoint for a thread.
    /// </summary>
    /// <param name="threadId">Thread id.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Checkpoint or null when missing.</returns>
    public Task<CheckpointSnapshot?> GetCheckpointAsync(
        string threadId,
        CancellationToken cancellationToken = default)
    {
        return Checkpointer.GetAsync(threadId, cancellationToken);
    }

    /// <summary>
    ///     Resumes an interrupted thread with a command (drains to terminal event).
    /// </summary>
    /// <param name="threadId">Thread id.</param>
    /// <param name="command">Resume command.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Terminal stream event.</returns>
    public async Task<StreamEvent> ResumeAsync(
        string threadId,
        Command command,
        CancellationToken cancellationToken = default)
    {
        TrackThread(threadId);
        return await Graph.ResumeInvokeAsync(threadId, command, cancellationToken);
    }

    /// <summary>
    ///     Streams resume events for an interrupted thread (SSE source).
    /// </summary>
    /// <param name="threadId">Thread id.</param>
    /// <param name="command">Resume command.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Live stream of graph events.</returns>
    public IAsyncEnumerable<StreamEvent> StreamResumeAsync(
        string threadId,
        Command command,
        CancellationToken cancellationToken = default)
    {
        TrackThread(threadId);
        return Graph.ResumeAsync(threadId, command, StreamMode.Events, cancellationToken);
    }

    /// <summary>
    ///     Streams a new invoke for a thread (SSE source).
    /// </summary>
    /// <param name="threadId">Thread id.</param>
    /// <param name="input">Seed channel writes.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Live stream of graph events.</returns>
    public IAsyncEnumerable<StreamEvent> StreamInvokeAsync(
        string threadId,
        IEnumerable<ChannelWrite> input,
        CancellationToken cancellationToken = default)
    {
        TrackThread(threadId);
        return Graph.StreamAsync(
            input,
            new RunOptions { ThreadId = threadId, StreamMode = StreamMode.Events },
            cancellationToken);
    }
}
