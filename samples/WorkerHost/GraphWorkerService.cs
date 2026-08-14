using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Voluta.Samples.WorkerHost;

/// <summary>
///     Durable runner: drains wakes, runs the graph until interrupt/done/fail, then waits for more work.
///     HITL can last hours — do not pin a graph turn to an HTTP request lifetime.
/// </summary>
public sealed class GraphWorkerService(
    ThreadWakeChannel wakes,
    GraphThreadRunner runner,
    ILogger<GraphWorkerService> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<string, byte> inFlight = new(StringComparer.Ordinal);

    /// <summary>
    ///     Outcomes observed this process lifetime (demo / tests).
    /// </summary>
    public ConcurrentBag<GraphThreadOutcome> Outcomes { get; } = [];

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Graph worker started — waiting for thread wakes");

        await foreach (var wake in wakes.ReadAllAsync(stoppingToken))
        {
            if (!inFlight.TryAdd(wake.ThreadId, 0))
            {
                logger.LogWarning(
                    "Skipping concurrent wake for {ThreadId} — already in-flight on this instance",
                    wake.ThreadId);
                continue;
            }

            try
            {
                var outcome = await runner.RunAsync(wake, stoppingToken);
                Outcomes.Add(outcome);
                ApplyDispositionPolicy(outcome);
            }
            finally
            {
                inFlight.TryRemove(wake.ThreadId, out _);
            }
        }

        logger.LogInformation("Graph worker stopped");
    }

    private void ApplyDispositionPolicy(GraphThreadOutcome outcome)
    {
        switch (outcome.Disposition)
        {
            case GraphThreadDisposition.Parked:
                // Checkpoint is SoT; another process may resume later via a new wake.
                logger.LogInformation(
                    "Policy: park {ThreadId} — wait for human / external resume wake",
                    outcome.ThreadId);
                break;

            case GraphThreadDisposition.Completed:
                logger.LogInformation("Policy: complete {ThreadId} — no further wakes needed", outcome.ThreadId);
                break;

            case GraphThreadDisposition.Failed:
                // Sample dead-letter: log + keep last-good checkpoint. Production: DLQ, alert, metrics.
                logger.LogError(
                    outcome.Exception,
                    "Policy: dead-letter {ThreadId} — do not ResumeInvokeAsync; re-invoke or rebuild from last-good",
                    outcome.ThreadId);
                break;

            case GraphThreadDisposition.Cancelled:
                logger.LogWarning("Policy: cancelled {ThreadId} — re-enqueue if work must continue", outcome.ThreadId);
                break;
        }
    }
}
