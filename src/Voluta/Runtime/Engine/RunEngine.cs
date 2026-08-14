using System.Diagnostics;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Diagnostics;
using Voluta.Exceptions;
using Voluta.Exceptions.Run;
using Voluta.Graph;
using Voluta.Runtime.Engine.Streaming;
using Voluta.Runtime.Engine.Support;
using Voluta.Runtime.Engine.Tasks;

// PendingInterrupt lives in Voluta.Abstractions.Checkpoint.

// GraphConstants lives in root Voluta namespace.
// CommandTaxonomy lives in Voluta.Runtime (parent of this namespace).

namespace Voluta.Runtime.Engine;

/// <summary>
///     Pregel superstep loop for a single stream/invoke/resume session.
/// </summary>
internal sealed class RunEngine(GraphTopology topology, ICheckpointer checkpointer)
{
    /// <summary>
    ///     Runs from input (or empty) until terminal, yielding stream events for the selected mode.
    /// </summary>
    public async IAsyncEnumerable<StreamEvent> StreamAsync(
        IEnumerable<ChannelWrite> input,
        RunOptions options,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var store = new ChannelStore(topology.Channels);
        var inputList = input as IList<ChannelWrite> ?? input.ToList();
        if (inputList.Count > 0)
        {
            store.ApplyInputWrites(inputList);
        }

        var nextNodes = RunEngineRouting.ResolveNextNodes(
            topology,
            GraphConstants.Start,
            store.SnapshotValues(),
            null);
        long step = 0;
        string? lastNode = null;

        await checkpointer.PutAsync(
            RunEngineSnapshots.Build(
                options.ThreadId,
                step,
                GraphRunStatus.Running,
                store,
                lastNode,
                nextNodes,
                [],
                null),
            cancellationToken);

        if (RunEngineStreaming.EmitsLifecycle(options.StreamMode))
        {
            yield return new StreamEvent
            {
                Mode = options.StreamMode,
                Kind = StreamEventKind.Start,
                Step = step
            };
        }

        await foreach (var item in RunLoopAsync(
                           options,
                           store,
                           RunEngineRouting.ToPullTasks(topology, nextNodes),
                           step,
                           lastNode,
                           resumeByTaskId: null,
                           cancellationToken))
        {
            yield return item;
        }
    }

    /// <summary>
    ///     Resumes an interrupted thread with a command payload.
    /// </summary>
    public async IAsyncEnumerable<StreamEvent> ResumeAsync(
        string threadId,
        Command command,
        StreamMode streamMode,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        Runtime.CommandTaxonomy.EnsureValid(command);

        var checkpoint = await checkpointer.GetAsync(threadId, cancellationToken) ??
                         throw new GraphInvalidResumeException(
                             $"No checkpoint found for thread '{threadId}'.");
        if (checkpoint.Status != GraphRunStatus.Interrupted)
        {
            throw new GraphInvalidResumeException(
                $"Thread '{threadId}' is not interrupted (status={checkpoint.Status}).");
        }

        var pendingInterrupts = RunEngineLoopHelpers.ResolvePendingInterrupts(checkpoint);
        Runtime.CommandTaxonomy.EnsureMultiInterruptResumes(command, pendingInterrupts);

        var store = new ChannelStore(topology.Channels);
        store.Restore(checkpoint.ChannelValues, checkpoint.ChannelVersions, checkpoint.VersionsSeen);

        if (command.Values is { Count: > 0 } values)
        {
            store.ApplyInputWrites(values.Select(pair => new ChannelWrite(pair.Key, pair.Value)));
        }

        var resumeByTaskId = RunEngineLoopHelpers.BuildResumeMap(command, pendingInterrupts);
        var readyTasks = RunEngineLoopHelpers.ToResumeReadyTasks(pendingInterrupts);
        if (readyTasks.Count == 0)
        {
            var nextNodes = checkpoint.NextNodes.Count > 0
                ? checkpoint.NextNodes
                : RunEngineRouting.ResolveNextNodes(
                    topology,
                    GraphConstants.Start,
                    store.SnapshotValues(),
                    command.Payload);
            readyTasks = RunEngineRouting.ToPullTasks(topology, nextNodes);
        }

        var options = new RunOptions { ThreadId = threadId, StreamMode = streamMode };

        if (RunEngineStreaming.EmitsLifecycle(streamMode))
        {
            yield return new StreamEvent
            {
                Mode = streamMode,
                Kind = StreamEventKind.Start,
                Step = checkpoint.Step
            };
        }

        await foreach (var item in RunLoopAsync(
                           options,
                           store,
                           readyTasks,
                           checkpoint.Step,
                           checkpoint.LastNode,
                           resumeByTaskId,
                           cancellationToken))
        {
            yield return item;
        }
    }

    /// <summary>
    ///     Continues a Running thread from the latest checkpoint (after UpdateState / Fork).
    ///     Does not re-inject HITL resume payload; use <see cref="ResumeAsync" /> for Interrupted.
    ///     Side-effect risk: nodes in NextNodes re-execute — hosts must make nodes idempotent.
    /// </summary>
    public async IAsyncEnumerable<StreamEvent> ContinueAsync(
        string threadId,
        StreamMode streamMode,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var checkpoint = await checkpointer.GetAsync(threadId, cancellationToken)
                         ?? throw new GraphThreadNotFoundException(
                             $"No checkpoint found for thread '{threadId}'.");
        if (checkpoint.Status != GraphRunStatus.Running)
        {
            throw new GraphInvalidContinueException(
                $"Thread '{threadId}' is not Running (status={checkpoint.Status}); " +
                "use ResumeAsync for Interrupted or UpdateStateAsync after Failed/Cancelled.");
        }

        var store = new ChannelStore(topology.Channels);
        store.Restore(checkpoint.ChannelValues, checkpoint.ChannelVersions, checkpoint.VersionsSeen);

        var nextNodes = checkpoint.NextNodes;
        if (nextNodes.Count == 0 && checkpoint.PendingSends.Count == 0)
        {
            throw new GraphInvalidContinueException(
                $"Thread '{threadId}' has no next nodes or pending sends to continue.");
        }

        var options = new RunOptions { ThreadId = threadId, StreamMode = streamMode };

        if (streamMode == StreamMode.Events)
        {
            yield return new StreamEvent
            {
                Mode = StreamMode.Events,
                Kind = StreamEventKind.Start,
                Step = checkpoint.Step
            };
        }

        // Prefer pull next-nodes; pending sends merge in the first superstep via checkpoint.PendingSends.
        if (checkpoint.PendingSends.Count > 0)
        {
            await foreach (var item in ContinueWithPendingSendsAsync(
                               options,
                               store,
                               nextNodes,
                               checkpoint,
                               cancellationToken))
            {
                yield return item;
            }

            yield break;
        }

        await foreach (var item in RunLoopAsync(
                           options,
                           store,
                           RunEngineRouting.ToPullTasks(topology, nextNodes),
                           checkpoint.Step,
                           checkpoint.LastNode,
                           resumeByTaskId: null,
                           cancellationToken))
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<StreamEvent> ContinueWithPendingSendsAsync(
        RunOptions options,
        ChannelStore store,
        IReadOnlyList<string> nextNodes,
        CheckpointSnapshot checkpoint,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        var pull = RunEngineRouting.ToPullTasks(topology, nextNodes);
        var ready = new List<ReadyTask>(pull.Count + checkpoint.PendingSends.Count);
        ready.AddRange(pull);
        foreach (var send in checkpoint.PendingSends)
        {
            ready.Add(new ReadyTask(send.NodeName, send.TaskId, send.Payload));
        }

        ready.Sort(static (left, right) =>
        {
            var nodeCompare = string.CompareOrdinal(left.NodeName, right.NodeName);
            return nodeCompare != 0
                ? nodeCompare
                : string.CompareOrdinal(left.TaskId, right.TaskId);
        });

        var readyTasks = (IReadOnlyList<ReadyTask>)ready;
        var step = checkpoint.Step;
        var lastNode = checkpoint.LastNode;
        var streamMode = options.StreamMode;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (readyTasks.Count == 0)
            {
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step,
                        GraphRunStatus.Done,
                        store,
                        lastNode,
                        [],
                        [],
                        interruptPayload: null),
                    cancellationToken);

                yield return RunEngineStreaming.Terminal(
                    streamMode,
                    StreamEventKind.End,
                    step,
                    store);
                yield break;
            }

            step++;
            if (step > topology.RecursionLimit)
            {
                var outOfSteps = new GraphOutOfStepsException(topology.RecursionLimit, step);
                var failedNodeNames = RunEngineLoopHelpers.DistinctNodeNames(readyTasks);
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step,
                        GraphRunStatus.Failed,
                        store,
                        lastNode,
                        failedNodeNames,
                        [],
                        null),
                    cancellationToken);

                yield return RunEngineStreaming.Terminal(
                    streamMode,
                    StreamEventKind.Failed,
                    step,
                    store,
                    outOfSteps);
                throw outOfSteps;
            }

            SuperstepCommit? commit = null;
            await foreach (var item in RunEngineLoopHelpers.RunSuperstepStreamingAsync(
                               topology,
                               checkpointer,
                               options,
                               store,
                               readyTasks,
                               step,
                               lastNode,
                               resumeByTaskId: null,
                               cancellationToken))
            {
                if (item.LiveEvent is { } live)
                {
                    yield return live;
                }

                if (item.Commit is { } done)
                {
                    commit = done;
                }
            }

            if (commit is null)
            {
                throw new InvalidOperationException("Superstep completed without a commit.");
            }

            lastNode = commit.LastNode;
            readyTasks = commit.ReadyTasks;

            if (commit.TerminalEvent is { } terminal)
            {
                yield return terminal;
                if (commit.Exception is not null)
                {
                    throw commit.Exception;
                }

                yield break;
            }

            foreach (var streamItem in commit.StreamItems)
            {
                yield return streamItem;
            }
        }
    }

    private async IAsyncEnumerable<StreamEvent> RunLoopAsync(
        RunOptions options,
        ChannelStore store,
        IReadOnlyList<ReadyTask> initialReady,
        long step,
        string? lastNode,
        IReadOnlyDictionary<string, object?>? resumeByTaskId,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        var readyTasks = initialReady;
        var isFirstResumeStep = resumeByTaskId is not null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (readyTasks.Count == 0)
            {
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step,
                        GraphRunStatus.Done,
                        store,
                        lastNode,
                        [],
                        [],
                        interruptPayload: null),
                    cancellationToken);

                yield return RunEngineStreaming.Terminal(
                    options.StreamMode,
                    StreamEventKind.End,
                    step,
                    store);
                yield break;
            }

            step++;
            if (step > topology.RecursionLimit)
            {
                var outOfSteps = new GraphOutOfStepsException(topology.RecursionLimit, step);
                var failedNodeNames = RunEngineLoopHelpers.DistinctNodeNames(readyTasks);
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step,
                        GraphRunStatus.Failed,
                        store,
                        lastNode,
                        failedNodeNames,
                        [],
                        null),
                    cancellationToken);

                yield return RunEngineStreaming.Terminal(
                    options.StreamMode,
                    StreamEventKind.Failed,
                    step,
                    store,
                    outOfSteps);
                throw outOfSteps;
            }

            // readyTasks from ToPullTasks are already sorted; re-sort only when sends merge in.
            var orderedReady = readyTasks;
            var payloadsForStep = isFirstResumeStep ? resumeByTaskId : null;
            isFirstResumeStep = false;

            // Superstep body runs concurrently with live Custom/Messages drain so tokens
            // surface before the superstep commit (async iterator cannot yield inside using).
            await foreach (var item in RunEngineLoopHelpers.RunSuperstepStreamingAsync(
                               topology,
                               checkpointer,
                               options,
                               store,
                               orderedReady,
                               step,
                               lastNode,
                               payloadsForStep,
                               cancellationToken))
            {
                if (item.Commit is { } commit)
                {
                    lastNode = commit.LastNode;
                    readyTasks = commit.ReadyTasks;

                    if (commit.TerminalEvent is { } terminal)
                    {
                        yield return terminal;
                        if (commit.Exception is not null)
                        {
                            throw commit.Exception;
                        }

                        yield break;
                    }

                    foreach (var streamItem in commit.StreamItems)
                    {
                        yield return streamItem;
                    }
                }
                else if (item.LiveEvent is { } live)
                {
                    yield return live;
                }
            }
        }
    }
}

/// <summary>
///     Result of one superstep body (after activity disposed, before stream yields).
/// </summary>
file sealed class SuperstepCommit
{
    public required string? LastNode { get; init; }

    public required IReadOnlyList<ReadyTask> ReadyTasks { get; init; }

    public StreamEvent? TerminalEvent { get; init; }

    public Exception? Exception { get; init; }

    public IReadOnlyList<StreamEvent> StreamItems { get; init; } = [];
}

/// <summary>
///     Live node stream item or final superstep commit (mutually exclusive).
/// </summary>
file sealed class SuperstepStreamItem
{
    public StreamEvent? LiveEvent { get; init; }

    public SuperstepCommit? Commit { get; init; }
}

/// <summary>
///     Hot-path helpers for ready-set node name extraction and superstep body (file-static).
/// </summary>
file static class RunEngineLoopHelpers
{
    public static async IAsyncEnumerable<SuperstepStreamItem> RunSuperstepStreamingAsync(
        GraphTopology topology,
        ICheckpointer checkpointer,
        RunOptions options,
        ChannelStore store,
        IReadOnlyList<ReadyTask> orderedReady,
        long step,
        string? lastNode,
        IReadOnlyDictionary<string, object?>? resumeByTaskId,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        var liveChannel = System.Threading.Channels.Channel.CreateUnbounded<StreamEvent>(
            new System.Threading.Channels.UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
            });

        Func<string, IStreamWriter>? streamWriterFactory = null;
        if (RunEngineStreaming.ForwardsNodeStreamItems(options.StreamMode))
        {
            streamWriterFactory = nodeName =>
                new ChannelStreamWriter(liveChannel.Writer, nodeName, step);
        }

        var executeTask = ExecuteSuperstepAsync(
            topology,
            checkpointer,
            options,
            store,
            orderedReady,
            step,
            lastNode,
            resumeByTaskId,
            options.ThreadId,
            streamWriterFactory,
            cancellationToken);

        while (!executeTask.IsCompleted)
        {
            while (liveChannel.Reader.TryRead(out var live))
            {
                yield return new SuperstepStreamItem { LiveEvent = live };
            }

            if (executeTask.IsCompleted)
            {
                break;
            }

            var waitRead = liveChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();
            var completed = await Task.WhenAny(executeTask, waitRead);
            if (completed == waitRead && await waitRead)
            {
                while (liveChannel.Reader.TryRead(out var live))
                {
                    yield return new SuperstepStreamItem { LiveEvent = live };
                }
            }
        }

        var commit = await executeTask;
        liveChannel.Writer.TryComplete();
        while (liveChannel.Reader.TryRead(out var remaining))
        {
            yield return new SuperstepStreamItem { LiveEvent = remaining };
        }

        yield return new SuperstepStreamItem { Commit = commit };
    }

    public static async Task<SuperstepCommit> ExecuteSuperstepAsync(
        GraphTopology topology,
        ICheckpointer checkpointer,
        RunOptions options,
        ChannelStore store,
        IReadOnlyList<ReadyTask> orderedReady,
        long step,
        string? lastNode,
        IReadOnlyDictionary<string, object?>? resumeByTaskId,
        string threadId,
        Func<string, IStreamWriter>? streamWriterFactory,
        CancellationToken cancellationToken)
    {
        using var superstep = ActivityScope.Start(
            VolutaDiagnostics.SuperstepActivityName,
            VolutaDiagnostics.SuperstepDuration);

        var preApplySnapshot = store.SnapshotValues();
        var executionOutcome = await RunEngineExecution.TryExecuteReadyAsync(
            topology,
            orderedReady,
            preApplySnapshot,
            resumeByTaskId,
            threadId,
            streamWriterFactory,
            cancellationToken);

        if (executionOutcome.Cancelled)
        {
            // Terminal marker at the failing superstep (not step-1): never clobber last-good.
            var cancelledNodes = DistinctNodeNames(orderedReady);
            await checkpointer.PutAsync(
                RunEngineSnapshots.Build(
                    options.ThreadId,
                    step,
                    GraphRunStatus.Cancelled,
                    store,
                    lastNode,
                    cancelledNodes,
                    [],
                    null),
                cancellationToken);

            superstep.SetTag(VolutaDiagnostics.TagRunStatus, nameof(GraphRunStatus.Cancelled));
            return new SuperstepCommit
            {
                LastNode = lastNode,
                ReadyTasks = orderedReady,
                TerminalEvent = RunEngineStreaming.Terminal(
                    options.StreamMode,
                    StreamEventKind.Cancelled,
                    step,
                    store),
                Exception = executionOutcome.Exception,
            };
        }

        if (executionOutcome.Failure is { } failure)
        {
            // Last-good payload: Failed at this superstep with store from last successful apply.
            var failedNodes = DistinctNodeNames(orderedReady);
            await checkpointer.PutAsync(
                RunEngineSnapshots.Build(
                    options.ThreadId,
                    step,
                    GraphRunStatus.Failed,
                    store,
                    lastNode,
                    failedNodes,
                    [],
                    null),
                cancellationToken);

            superstep.SetError(failure);
            superstep.SetTag(VolutaDiagnostics.TagRunStatus, nameof(GraphRunStatus.Failed));
            return new SuperstepCommit
            {
                LastNode = lastNode,
                ReadyTasks = orderedReady,
                TerminalEvent = RunEngineStreaming.Terminal(
                    options.StreamMode,
                    StreamEventKind.Failed,
                    step,
                    store,
                    failure),
                Exception = failure,
            };
        }

        var executions = executionOutcome.Executions!;
        var pendingInterrupts = CollectPendingInterrupts(orderedReady, executions);
        if (pendingInterrupts.Count > 0)
        {
            // When any task interrupts, continue results from the same superstep are not applied
            // (barrier holds until all pending interrupts resume).
            lastNode = pendingInterrupts[^1].NodeName;
            var nextNodeNames = DistinctNames(
                pendingInterrupts.Select(static item => item.NodeName).ToList());
            var primaryPayload = pendingInterrupts[0].Payload;
            await checkpointer.PutAsync(
                RunEngineSnapshots.Build(
                    options.ThreadId,
                    step,
                    GraphRunStatus.Interrupted,
                    store,
                    lastNode,
                    nextNodeNames,
                    [],
                    primaryPayload,
                    pendingInterrupts: pendingInterrupts),
                cancellationToken);

            superstep.SetTag(VolutaDiagnostics.TagRunStatus, nameof(GraphRunStatus.Interrupted));
            superstep.SetTag(VolutaDiagnostics.TagNodeName, lastNode);
            return new SuperstepCommit
            {
                LastNode = lastNode,
                ReadyTasks = orderedReady,
                TerminalEvent = new StreamEvent
                {
                    Mode = options.StreamMode,
                    Kind = StreamEventKind.Interrupt,
                    Step = step,
                    NodeNames = nextNodeNames,
                    Payload = pendingInterrupts.Count == 1
                        ? primaryPayload
                        : pendingInterrupts,
                    State = options.StreamMode == StreamMode.Values ? store.SnapshotValues() : null,
                },
            };
        }

        var writes = RunEngineExecution.CollectWrites(executions);
        var applyError = RunEngineExecution.TryApplyWrites(store, writes);
        if (applyError is not null)
        {
            // Merge never applied — store still last-good; Failed at this step keeps history.
            var applyFailedNodes = DistinctNodeNames(orderedReady);
            await checkpointer.PutAsync(
                RunEngineSnapshots.Build(
                    options.ThreadId,
                    step,
                    GraphRunStatus.Failed,
                    store,
                    lastNode,
                    applyFailedNodes,
                    [],
                    null),
                cancellationToken);

            superstep.SetError(applyError);
            superstep.SetTag(VolutaDiagnostics.TagRunStatus, nameof(GraphRunStatus.Failed));
            return new SuperstepCommit
            {
                LastNode = lastNode,
                ReadyTasks = orderedReady,
                TerminalEvent = RunEngineStreaming.Terminal(
                    options.StreamMode,
                    StreamEventKind.Failed,
                    step,
                    store,
                    applyError),
                Exception = applyError,
            };
        }

        // One post-apply snapshot shared by routing, checkpoint, and Values stream.
        var postApplySnapshot = store.SnapshotValues();
        var nodeNames = DistinctNodeNames(orderedReady);
        foreach (var nodeName in nodeNames)
        {
            store.MarkSeen(nodeName);
        }

        lastNode = orderedReady[^1].NodeName;
        var scheduled = new List<string>(executions.Count);
        var pendingSends = new List<PendingSend>();
        foreach (var execution in executions)
        {
            scheduled.AddRange(
                RunEngineRouting.ResolveNextNodes(
                    topology,
                    execution.NodeName,
                    postApplySnapshot,
                    null));

            if (execution.Result is ContinueNodeResult continueResult)
            {
                foreach (var send in continueResult.Sends)
                {
                    if (!topology.Nodes.ContainsKey(send.Node))
                    {
                        var sendError = new GraphRunFailedException(
                            $"Send targets unknown node '{send.Node}' from '{execution.NodeName}'.");
                        superstep.SetError(sendError);
                        throw sendError;
                    }

                    pendingSends.Add(
                        new PendingSend
                        {
                            NodeName = send.Node,
                            Payload = send.Payload,
                            TaskId = $"{execution.NodeName}->{send.Node}:{pendingSends.Count}",
                        });
                }
            }
        }

        var nextPull = RunEngineRouting.ToPullTasks(topology, DistinctNames(scheduled));
        IReadOnlyList<ReadyTask> readyTasks;
        if (pendingSends.Count == 0)
        {
            readyTasks = nextPull;
        }
        else
        {
            // Merge pull (already sorted) with sends; sort once by node then task id.
            var merged = new List<ReadyTask>(nextPull.Count + pendingSends.Count);
            merged.AddRange(nextPull);
            foreach (var send in pendingSends)
            {
                merged.Add(new ReadyTask(send.NodeName, send.TaskId, send.Payload));
            }

            merged.Sort(static (left, right) =>
            {
                var nodeCompare = string.CompareOrdinal(left.NodeName, right.NodeName);
                return nodeCompare != 0
                    ? nodeCompare
                    : string.CompareOrdinal(left.TaskId, right.TaskId);
            });
            readyTasks = merged;
        }

        var checkpointNextNodes = DistinctNodeNames(readyTasks);
        await checkpointer.PutAsync(
            RunEngineSnapshots.Build(
                options.ThreadId,
                step,
                GraphRunStatus.Running,
                store,
                lastNode,
                checkpointNextNodes,
                pendingSends,
                null,
                postApplySnapshot),
            cancellationToken);

        superstep.SetTag(VolutaDiagnostics.TagRunStatus, nameof(GraphRunStatus.Running));
        return new SuperstepCommit
        {
            LastNode = lastNode,
            ReadyTasks = readyTasks,
            StreamItems = RunEngineStreaming.EmitCommit(
                options.StreamMode,
                step,
                nodeNames,
                writes,
                store,
                postApplySnapshot).ToList(),
        };
    }

    public static IReadOnlyList<string> DistinctNodeNames(IReadOnlyList<ReadyTask> tasks)
    {
        if (tasks.Count == 0)
        {
            return [];
        }

        if (tasks.Count == 1)
        {
            return [tasks[0].NodeName];
        }

        // Ready tasks are ordered by NodeName — adjacent de-dupe is O(n).
        var names = new List<string>(tasks.Count);
        string? previous = null;
        for (var index = 0; index < tasks.Count; index++)
        {
            var name = tasks[index].NodeName;
            if (previous is null || !string.Equals(previous, name, StringComparison.Ordinal))
            {
                names.Add(name);
                previous = name;
            }
        }

        return names;
    }

    public static IReadOnlyList<string> DistinctNames(List<string> names)
    {
        if (names.Count == 0)
        {
            return [];
        }

        if (names.Count == 1)
        {
            return names;
        }

        var seen = new HashSet<string>(names.Count, StringComparer.Ordinal);
        var distinct = new List<string>(names.Count);
        foreach (var name in names)
        {
            if (seen.Add(name))
            {
                distinct.Add(name);
            }
        }

        return distinct;
    }

    public static IReadOnlyList<PendingInterrupt> ResolvePendingInterrupts(CheckpointSnapshot checkpoint)
    {
        if (checkpoint.PendingInterrupts is { Count: > 0 } pending)
        {
            return pending;
        }

        // Legacy single-path: only InterruptPayload + NextNodes/LastNode.
        if (checkpoint.InterruptPayload is null && checkpoint.NextNodes.Count == 0)
        {
            return [];
        }

        var nodeName = checkpoint.NextNodes.Count > 0
            ? checkpoint.NextNodes[0]
            : checkpoint.LastNode ?? GraphConstants.Start;
        return
        [
            new PendingInterrupt
            {
                TaskId = nodeName,
                NodeName = nodeName,
                Payload = checkpoint.InterruptPayload,
            },
        ];
    }

    public static Dictionary<string, object?> BuildResumeMap(
        Command command,
        IReadOnlyList<PendingInterrupt> pendingInterrupts)
    {
        if (command.Resumes is { Count: > 0 } resumes)
        {
            return new Dictionary<string, object?>(resumes, StringComparer.Ordinal);
        }

        // Single-path: same payload for every pending interrupt (typically one).
        var map = new Dictionary<string, object?>(pendingInterrupts.Count, StringComparer.Ordinal);
        foreach (var pending in pendingInterrupts)
        {
            map[pending.TaskId] = command.Payload;
        }

        return map;
    }

    public static IReadOnlyList<ReadyTask> ToResumeReadyTasks(IReadOnlyList<PendingInterrupt> pendingInterrupts)
    {
        if (pendingInterrupts.Count == 0)
        {
            return [];
        }

        var tasks = new List<ReadyTask>(pendingInterrupts.Count);
        foreach (var pending in pendingInterrupts)
        {
            tasks.Add(new ReadyTask(pending.NodeName, pending.TaskId, pending.TaskPayload));
        }

        tasks.Sort(static (left, right) =>
        {
            var nodeCompare = string.CompareOrdinal(left.NodeName, right.NodeName);
            return nodeCompare != 0
                ? nodeCompare
                : string.CompareOrdinal(left.TaskId, right.TaskId);
        });
        return tasks;
    }

    public static IReadOnlyList<PendingInterrupt> CollectPendingInterrupts(
        IReadOnlyList<ReadyTask> orderedReady,
        IReadOnlyList<NodeExecution> executions)
    {
        // Executions preserve orderedReady order (WhenAll / single path).
        List<PendingInterrupt>? pending = null;
        for (var index = 0; index < executions.Count; index++)
        {
            if (executions[index].Result is not InterruptNodeResult interruptResult)
            {
                continue;
            }

            pending ??= [];
            var ready = orderedReady[index];
            pending.Add(
                new PendingInterrupt
                {
                    TaskId = ready.TaskId,
                    NodeName = ready.NodeName,
                    Payload = interruptResult.Payload,
                    TaskPayload = ready.TaskPayload,
                });
        }

        return pending is null ? [] : pending;
    }
}
