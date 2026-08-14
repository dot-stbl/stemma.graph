using Microsoft.Extensions.Logging;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Graph;

namespace Voluta.Samples.WorkerHost;

/// <summary>
///     Runs one wake against a compiled graph: invoke or resume until interrupt/done/fail.
/// </summary>
public sealed class GraphThreadRunner(CompiledGraph graph, ILogger<GraphThreadRunner> logger)
{
    /// <summary>
    ///     Processes a single wake to a terminal disposition.
    /// </summary>
    public async Task<GraphThreadOutcome> RunAsync(ThreadWake wake, CancellationToken cancellationToken = default)
    {
        try
        {
            var terminal = wake.Command is { } command
                ? await graph.ResumeInvokeAsync(wake.ThreadId, command, cancellationToken)
                : await graph.InvokeAsync(
                    wake.Input ?? [],
                    new RunOptions { ThreadId = wake.ThreadId, StreamMode = StreamMode.Events },
                    cancellationToken);

            return MapTerminal(wake.ThreadId, terminal);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Thread {ThreadId} turn cancelled", wake.ThreadId);
            return new GraphThreadOutcome
            {
                ThreadId = wake.ThreadId,
                Disposition = GraphThreadDisposition.Cancelled,
            };
        }
        catch (Exception exception)
        {
            // Invoke/Resume rethrow graph faults after writing Failed checkpoint (last-good policy).
            logger.LogError(
                exception,
                "Thread {ThreadId} failed — checkpoint is terminal Failed; do not HITL-resume",
                wake.ThreadId);
            return new GraphThreadOutcome
            {
                ThreadId = wake.ThreadId,
                Disposition = GraphThreadDisposition.Failed,
                Exception = exception,
            };
        }
    }

    private GraphThreadOutcome MapTerminal(string threadId, StreamEvent terminal)
    {
        return terminal.Kind switch
        {
            StreamEventKind.Interrupt => Park(threadId, terminal),
            StreamEventKind.End => Complete(threadId, terminal),
            StreamEventKind.Failed => Fail(threadId, terminal, terminal.Payload as Exception),
            StreamEventKind.Cancelled => new GraphThreadOutcome
            {
                ThreadId = threadId,
                Disposition = GraphThreadDisposition.Cancelled,
                Terminal = terminal,
            },
            _ => Fail(
                threadId,
                terminal,
                new InvalidOperationException($"Unexpected terminal kind {terminal.Kind} for thread {threadId}.")),
        };
    }

    private GraphThreadOutcome Park(string threadId, StreamEvent terminal)
    {
        logger.LogInformation(
            "Thread {ThreadId} parked at interrupt (step {Step})",
            threadId,
            terminal.Step);
        return new GraphThreadOutcome
        {
            ThreadId = threadId,
            Disposition = GraphThreadDisposition.Parked,
            Terminal = terminal,
        };
    }

    private GraphThreadOutcome Complete(string threadId, StreamEvent terminal)
    {
        logger.LogInformation("Thread {ThreadId} completed", threadId);
        return new GraphThreadOutcome
        {
            ThreadId = threadId,
            Disposition = GraphThreadDisposition.Completed,
            Terminal = terminal,
        };
    }

    private GraphThreadOutcome Fail(string threadId, StreamEvent? terminal, Exception? exception)
    {
        logger.LogError(
            exception,
            "Thread {ThreadId} failed at stream terminal — apply dead-letter / alert policy here",
            threadId);
        return new GraphThreadOutcome
        {
            ThreadId = threadId,
            Disposition = GraphThreadDisposition.Failed,
            Terminal = terminal,
            Exception = exception,
        };
    }
}
