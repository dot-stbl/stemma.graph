using Voluta.Abstractions.Results;
using Voluta.Exceptions;
using Voluta.Exceptions.Run;
using Voluta.Graph;
using Voluta.Runtime.Engine.Tasks;

namespace Voluta.Runtime.Engine.Support;

/// <summary>
///     Parallel node execution helpers.
/// </summary>
internal static class RunEngineExecution
{
    public static async Task<ReadyExecutionOutcome> TryExecuteReadyAsync(
        GraphTopology topology,
        IReadOnlyList<ReadyTask> orderedReady,
        IReadOnlyDictionary<string, object?> snapshot,
        object? resumePayload,
        CancellationToken cancellationToken)
    {
        try
        {
            var tasks = orderedReady.Select(async readyTask =>
            {
                if (!topology.Nodes.TryGetValue(readyTask.NodeName, out var handler))
                {
                    throw new GraphRunFailedException($"Unknown ready node '{readyTask.NodeName}'.");
                }

                var context = new GraphContext(
                    readyTask.NodeName,
                    snapshot,
                    resumePayload,
                    readyTask.TaskPayload);
                try
                {
                    var result = await handler(context, cancellationToken);
                    return new NodeExecution(readyTask.NodeName, readyTask.TaskId, result);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception) when (exception is not GraphException)
                {
                    throw new GraphRunFailedException(
                        $"Node '{readyTask.NodeName}' threw: {exception.Message}",
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
        IReadOnlyList<TaskChannelWrite> writes)
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

    public static IReadOnlyList<TaskChannelWrite> CollectWrites(IReadOnlyList<NodeExecution> executions)
    {
        var writes = new List<TaskChannelWrite>();
        foreach (var execution in executions
                     .OrderBy(static item => item.NodeName, StringComparer.Ordinal)
                     .ThenBy(static item => item.TaskId, StringComparer.Ordinal))
        {
            if (execution.Result is not ContinueNodeResult continueResult)
            {
                continue;
            }

            foreach (var write in continueResult.Writes)
            {
                writes.Add(new TaskChannelWrite(execution.TaskId, write));
            }
        }

        return writes;
    }
}
