using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Exceptions;
using Voluta.Exceptions.Run;
using Voluta.Graph;
using Voluta.Runtime.Engine.Streaming;
using Voluta.Runtime.Engine.Support;
using Voluta.Runtime.Engine.Tasks;

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

        if (options.StreamMode == StreamMode.Events)
        {
            yield return new StreamEvent
            {
                Mode = StreamMode.Events,
                Kind = StreamEventKind.Start,
                Step = step
            };
        }

        await foreach (var item in RunLoopAsync(
                           options,
                           store,
                           nextNodes,
                           step,
                           lastNode,
                           null,
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

        var store = new ChannelStore(topology.Channels);
        store.Restore(checkpoint.ChannelValues, checkpoint.ChannelVersions, checkpoint.VersionsSeen);

        if (command.Values is { Count: > 0 } values)
        {
            store.ApplyInputWrites(values.Select(pair => new ChannelWrite(pair.Key, pair.Value)));
        }

        var nextNodes = checkpoint.NextNodes.Count > 0
            ? checkpoint.NextNodes
            : RunEngineRouting.ResolveNextNodes(
                topology,
                GraphConstants.Start,
                store.SnapshotValues(),
                command.Payload);

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

        await foreach (var item in RunLoopAsync(
                           options,
                           store,
                           nextNodes,
                           checkpoint.Step,
                           checkpoint.LastNode,
                           command.Payload,
                           cancellationToken))
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<StreamEvent> RunLoopAsync(
        RunOptions options,
        ChannelStore store,
        IReadOnlyList<string> nextNodes,
        long step,
        string? lastNode,
        object? resumePayload,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        var readyTasks = RunEngineRouting.ToPullTasks(topology, nextNodes);
        var isFirstResumeStep = resumePayload is not null;

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

            // Pre-apply snapshot for node handlers (barrier visibility).
            var preApplySnapshot = store.SnapshotValues();
            // readyTasks from ToPullTasks are already sorted; re-sort only when sends merge in.
            var orderedReady = readyTasks;
            var payloadForStep = isFirstResumeStep ? resumePayload : null;
            isFirstResumeStep = false;

            var executionOutcome = await RunEngineExecution.TryExecuteReadyAsync(
                topology,
                orderedReady,
                preApplySnapshot,
                payloadForStep,
                cancellationToken);

            if (executionOutcome.Cancelled)
            {
                var cancelledNodes = RunEngineLoopHelpers.DistinctNodeNames(orderedReady);
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step - 1,
                        GraphRunStatus.Cancelled,
                        store,
                        lastNode,
                        cancelledNodes,
                        [],
                        null),
                    cancellationToken);

                yield return RunEngineStreaming.Terminal(
                    options.StreamMode,
                    StreamEventKind.Cancelled,
                    step,
                    store);
                throw executionOutcome.Exception!;
            }

            if (executionOutcome.Failure is { } failure)
            {
                var failedNodes = RunEngineLoopHelpers.DistinctNodeNames(orderedReady);
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step - 1,
                        GraphRunStatus.Failed,
                        store,
                        lastNode,
                        failedNodes,
                        [],
                        null),
                    cancellationToken);

                yield return RunEngineStreaming.Terminal(
                    options.StreamMode,
                    StreamEventKind.Failed,
                    step,
                    store,
                    failure);
                throw failure;
            }

            var executions = executionOutcome.Executions!;
            if (executions.FirstOrDefault(item => item.Result is InterruptNodeResult) is { } interrupted)
            {
                var interruptResult = (InterruptNodeResult)interrupted.Result;
                lastNode = interrupted.NodeName;
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step,
                        GraphRunStatus.Interrupted,
                        store,
                        lastNode,
                        [interrupted.NodeName],
                        [],
                        interruptResult.Payload),
                    cancellationToken);

                yield return new StreamEvent
                {
                    Mode = options.StreamMode,
                    Kind = StreamEventKind.Interrupt,
                    Step = step,
                    NodeNames = [interrupted.NodeName],
                    Payload = interruptResult.Payload,
                    State = options.StreamMode == StreamMode.Values ? store.SnapshotValues() : null
                };
                yield break;
            }

            var writes = RunEngineExecution.CollectWrites(executions);
            var applyError = RunEngineExecution.TryApplyWrites(store, writes);
            if (applyError is not null)
            {
                var applyFailedNodes = RunEngineLoopHelpers.DistinctNodeNames(orderedReady);
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step - 1,
                        GraphRunStatus.Failed,
                        store,
                        lastNode,
                        applyFailedNodes,
                        [],
                        null),
                    cancellationToken);

                yield return RunEngineStreaming.Terminal(
                    options.StreamMode,
                    StreamEventKind.Failed,
                    step,
                    store,
                    applyError);
                throw applyError;
            }

            // One post-apply snapshot shared by routing, checkpoint, and Values stream.
            var postApplySnapshot = store.SnapshotValues();
            var nodeNames = RunEngineLoopHelpers.DistinctNodeNames(orderedReady);
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
                            throw new GraphRunFailedException(
                                $"Send targets unknown node '{send.Node}' from '{execution.NodeName}'.");
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

            var nextPull = RunEngineRouting.ToPullTasks(
                topology,
                RunEngineLoopHelpers.DistinctNames(scheduled));
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

            var checkpointNextNodes = RunEngineLoopHelpers.DistinctNodeNames(readyTasks);
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

            foreach (var streamItem in RunEngineStreaming.EmitCommit(
                         options.StreamMode,
                         step,
                         nodeNames,
                         writes,
                         store,
                         postApplySnapshot))
            {
                yield return streamItem;
            }
        }
    }
}

/// <summary>
///     Hot-path helpers for ready-set node name extraction (file-static).
/// </summary>
file static class RunEngineLoopHelpers
{
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
}
