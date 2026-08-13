// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Channels;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph;
using StemmaGraph.Results;
using StemmaGraph.Runtime.Exceptions;
using StemmaGraph.Streaming;

// GraphConstants lives in root StemmaGraph namespace.

namespace StemmaGraph.Runtime;

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
        var inputList = input.ToList();
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
            ? [.. checkpoint.NextNodes]
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
        List<string> nextNodes,
        long step,
        string? lastNode,
        object? resumePayload,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        var ready = RunEngineRouting.FilterRunnableNodes(topology, nextNodes);
        var isFirstResumeStep = resumePayload is not null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ready.Count == 0)
            {
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step,
                        GraphRunStatus.Done,
                        store,
                        lastNode,
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
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step,
                        GraphRunStatus.Failed,
                        store,
                        lastNode,
                        ready,
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

            var snapshot = store.SnapshotValues();
            var orderedReady = ready.OrderBy(name => name, StringComparer.Ordinal).ToList();
            var payloadForStep = isFirstResumeStep ? resumePayload : null;
            isFirstResumeStep = false;

            var executionOutcome = await RunEngineExecution.TryExecuteReadyAsync(
                topology,
                orderedReady,
                snapshot,
                payloadForStep,
                cancellationToken);

            if (executionOutcome.Cancelled)
            {
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step - 1,
                        GraphRunStatus.Cancelled,
                        store,
                        lastNode,
                        orderedReady,
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
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step - 1,
                        GraphRunStatus.Failed,
                        store,
                        lastNode,
                        orderedReady,
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
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step - 1,
                        GraphRunStatus.Failed,
                        store,
                        lastNode,
                        orderedReady,
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

            foreach (var nodeName in orderedReady)
            {
                store.MarkSeen(nodeName);
            }

            lastNode = orderedReady[^1];
            var scheduled = new List<string>();
            foreach (var nodeName in orderedReady)
            {
                scheduled.AddRange(
                    RunEngineRouting.ResolveNextNodes(
                        topology,
                        nodeName,
                        store.SnapshotValues(),
                        null));
            }

            ready = RunEngineRouting.FilterRunnableNodes(
                topology,
                [.. scheduled.Distinct(StringComparer.Ordinal)]);

            await checkpointer.PutAsync(
                RunEngineSnapshots.Build(
                    options.ThreadId,
                    step,
                    GraphRunStatus.Running,
                    store,
                    lastNode,
                    ready,
                    null),
                cancellationToken);

            foreach (var streamItem in RunEngineStreaming.EmitCommit(
                         options.StreamMode,
                         step,
                         orderedReady,
                         writes,
                         store))
            {
                yield return streamItem;
            }
        }
    }
}

/// <summary>
///     Node execution result pair for one superstep task.
/// </summary>
internal sealed class NodeExecution(string nodeName, NodeResult result)
{
    public string NodeName { get; } = nodeName;

    public NodeResult Result { get; } = result;
}

/// <summary>
///     Ready-set routing helpers.
/// </summary>
file static class RunEngineRouting
{
    public static List<string> ResolveNextNodes(
        GraphTopology topology,
        string source,
        IReadOnlyDictionary<string, object?> channelValues,
        object? resumePayload)
    {
        if (topology.ConditionalEdges.TryGetValue(source, out var router))
        {
            var context = new GraphContext(source, channelValues, resumePayload);
            return [.. router(context)];
        }

        return topology.StaticEdges.TryGetValue(source, out var targets)
            ? [.. targets]
            : [];
    }

    public static List<string> FilterRunnableNodes(GraphTopology topology, IReadOnlyList<string> candidates)
    {
        return
        [
            .. candidates
                .Where(name => name != GraphConstants.End && topology.Nodes.ContainsKey(name))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
        ];
    }
}

/// <summary>
///     Outcome of attempting to execute the ready set.
/// </summary>
internal sealed class ReadyExecutionOutcome
{
    public bool Cancelled { get; init; }

    public Exception? Exception { get; init; }

    public GraphException? Failure { get; init; }

    public List<NodeExecution>? Executions { get; init; }
}

/// <summary>
///     Parallel node execution helpers.
/// </summary>
file static class RunEngineExecution
{
    public static async Task<ReadyExecutionOutcome> TryExecuteReadyAsync(
        GraphTopology topology,
        IReadOnlyList<string> orderedReady,
        IReadOnlyDictionary<string, object?> snapshot,
        object? resumePayload,
        CancellationToken cancellationToken)
    {
        try
        {
            var tasks = orderedReady.Select(async nodeName =>
            {
                if (!topology.Nodes.TryGetValue(nodeName, out var handler))
                {
                    throw new GraphRunFailedException($"Unknown ready node '{nodeName}'.");
                }

                var context = new GraphContext(nodeName, snapshot, resumePayload);
                try
                {
                    var result = await handler(context, cancellationToken);
                    return new NodeExecution(nodeName, result);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception) when (exception is not GraphException)
                {
                    throw new GraphRunFailedException(
                        $"Node '{nodeName}' threw: {exception.Message}",
                        exception);
                }
            });

            return new ReadyExecutionOutcome
            {
                Executions = [.. await Task.WhenAll(tasks)]
            };
        }
        catch (OperationCanceledException exception)
        {
            return new ReadyExecutionOutcome
            {
                Cancelled = true,
                Exception = exception
            };
        }
        catch (GraphException exception)
        {
            return new ReadyExecutionOutcome { Failure = exception };
        }
        catch (Exception exception)
        {
            return new ReadyExecutionOutcome
            {
                Failure = new GraphRunFailedException(
                    $"Node execution failed: {exception.Message}",
                    exception)
            };
        }
    }

    public static GraphConcurrentUpdateException? TryApplyWrites(
        ChannelStore store,
        IReadOnlyList<(string TaskId, ChannelWrite Write)> writes)
    {
        try
        {
            store.ApplyWrites(writes);
            return null;
        }
        catch (GraphConcurrentUpdateException exception)
        {
            return exception;
        }
    }

    public static List<(string TaskId, ChannelWrite Write)> CollectWrites(IReadOnlyList<NodeExecution> executions)
    {
        var writes = new List<(string TaskId, ChannelWrite Write)>();
        foreach (var execution in executions.OrderBy(static item => item.NodeName, StringComparer.Ordinal))
        {
            if (execution.Result is not ContinueNodeResult continueResult)
            {
                continue;
            }

            foreach (var write in continueResult.Writes)
            {
                writes.Add((execution.NodeName, write));
            }
        }

        return writes;
    }
}

/// <summary>
///     Checkpoint snapshot builders for the run engine.
/// </summary>
file static class RunEngineSnapshots
{
    public static CheckpointSnapshot Build(
        string threadId,
        long step,
        GraphRunStatus status,
        ChannelStore store,
        string? lastNode,
        IReadOnlyList<string> nextNodes,
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
            LastNode = lastNode,
            NextNodes = [.. nextNodes],
            InterruptPayload = interruptPayload
        };
    }
}

/// <summary>
///     Stream event emission helpers.
/// </summary>
file static class RunEngineStreaming
{
    public static IEnumerable<StreamEvent> EmitCommit(
        StreamMode mode,
        long step,
        IReadOnlyList<string> nodeNames,
        IReadOnlyList<(string TaskId, ChannelWrite Write)> writes,
        ChannelStore store)
    {
        if (mode == StreamMode.Updates)
        {
            yield return new StreamEvent
            {
                Mode = StreamMode.Updates,
                Kind = StreamEventKind.Updates,
                Step = step,
                NodeNames = [.. nodeNames],
                Writes = [.. writes.Select(static item => item.Write)]
            };
        }
        else if (mode == StreamMode.Values)
        {
            yield return new StreamEvent
            {
                Mode = StreamMode.Values,
                Kind = StreamEventKind.Values,
                Step = step,
                NodeNames = [.. nodeNames],
                State = store.SnapshotValues()
            };
        }
    }

    public static StreamEvent Terminal(
        StreamMode mode,
        StreamEventKind kind,
        long step,
        ChannelStore store,
        object? payload = null)
    {
        return new StreamEvent
        {
            Mode = mode == StreamMode.Events ? StreamMode.Events : mode,
            Kind = kind,
            Step = step,
            State = mode == StreamMode.Values ? store.SnapshotValues() : null,
            Payload = payload
        };
    }
}
