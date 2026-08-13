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
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step,
                        GraphRunStatus.Failed,
                        store,
                        lastNode,
                        [.. readyTasks.Select(static task => task.NodeName).Distinct(StringComparer.Ordinal)],
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

            var snapshot = store.SnapshotValues();
            var orderedReady = readyTasks
                .OrderBy(static task => task.NodeName, StringComparer.Ordinal)
                .ThenBy(static task => task.TaskId, StringComparer.Ordinal)
                .ToList();
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
                        [.. orderedReady.Select(static task => task.NodeName).Distinct(StringComparer.Ordinal)],
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
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step - 1,
                        GraphRunStatus.Failed,
                        store,
                        lastNode,
                        [.. orderedReady.Select(static task => task.NodeName).Distinct(StringComparer.Ordinal)],
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
                await checkpointer.PutAsync(
                    RunEngineSnapshots.Build(
                        options.ThreadId,
                        step - 1,
                        GraphRunStatus.Failed,
                        store,
                        lastNode,
                        [.. orderedReady.Select(static task => task.NodeName).Distinct(StringComparer.Ordinal)],
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

            foreach (var nodeName in orderedReady.Select(static task => task.NodeName).Distinct(StringComparer.Ordinal))
            {
                store.MarkSeen(nodeName);
            }

            lastNode = orderedReady[^1].NodeName;
            var scheduled = new List<string>();
            var pendingSends = new List<PendingSend>();
            foreach (var execution in executions)
            {
                scheduled.AddRange(
                    RunEngineRouting.ResolveNextNodes(
                        topology,
                        execution.NodeName,
                        store.SnapshotValues(),
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
                [.. scheduled.Distinct(StringComparer.Ordinal)]);
            readyTasks =
            [
                .. nextPull,
                .. pendingSends.Select(static send => new ReadyTask(
                    send.NodeName,
                    send.TaskId,
                    send.Payload)),
            ];

            await checkpointer.PutAsync(
                RunEngineSnapshots.Build(
                    options.ThreadId,
                    step,
                    GraphRunStatus.Running,
                    store,
                    lastNode,
                    [.. readyTasks.Select(static task => task.NodeName).Distinct(StringComparer.Ordinal)],
                    pendingSends,
                    null),
                cancellationToken);

            foreach (var streamItem in RunEngineStreaming.EmitCommit(
                         options.StreamMode,
                         step,
                         [.. orderedReady.Select(static task => task.NodeName).Distinct(StringComparer.Ordinal)],
                         writes,
                         store))
            {
                yield return streamItem;
            }
        }
    }
}
