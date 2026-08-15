using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Voluta.Hosting.Wake;

namespace Voluta.Hosting.Worker;

/// <summary>
///     Durable runner: drains wakes, runs the graph until interrupt/done/fail, then waits for more work.
///     HITL can last hours — do not pin a graph turn to an HTTP request lifetime.
/// </summary>
/// <remarks>
///     Turns for different <c>threadId</c> values may run concurrently. A second wake for an
///     already in-flight thread on <strong>this process</strong> is skipped. Across replicas, use a
///     shared durable checkpointer and partition/lease wakes by thread id. The checkpointer is
///     the source of truth; wakes are hints.
/// </remarks>
public sealed class GraphWorkerService : BackgroundService
{
    private readonly ConcurrentDictionary<string, byte> inFlight = new(StringComparer.Ordinal);
    private readonly ILogger<GraphWorkerService> logger;
    private readonly GraphThreadRunner runner;
    private readonly IThreadWakeBus wakes;

    /// <summary>
    ///     Creates a worker that drains <paramref name="wakes" /> via <paramref name="runner" />.
    /// </summary>
    /// <param name="wakes">Wake bus.</param>
    /// <param name="runner">Per-wake graph runner.</param>
    /// <param name="logger">Logger.</param>
    public GraphWorkerService(
        IThreadWakeBus wakes,
        GraphThreadRunner runner,
        ILogger<GraphWorkerService> logger)
    {
        this.wakes = wakes;
        this.runner = runner;
        this.logger = logger;
    }

    /// <summary>
    ///     Outcomes observed this process lifetime (samples / tests / diagnostics).
    /// </summary>
    public ConcurrentBag<GraphThreadOutcome> Outcomes { get; } = [];

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Graph worker started — waiting for thread wakes");

        var pending = new ConcurrentBag<Task>();

        await foreach (var wake in wakes.ReadAllAsync(stoppingToken))
        {
            if (!inFlight.TryAdd(wake.ThreadId, 0))
            {
                logger.LogWarning(
                    "Skipping concurrent wake for {ThreadId} — already in-flight on this instance",
                    wake.ThreadId);
                continue;
            }

            pending.Add(GraphWorkerTurn.RunAsync(wake, runner, Outcomes, inFlight, logger, stoppingToken));
        }

        await Task.WhenAll(pending);

        logger.LogInformation("Graph worker stopped");
    }
}

/// <summary>
///     Executes a single wake turn and records disposition.
/// </summary>
file static class GraphWorkerTurn
{
    public static async Task RunAsync(
        ThreadWake wake,
        GraphThreadRunner runner,
        ConcurrentBag<GraphThreadOutcome> outcomes,
        ConcurrentDictionary<string, byte> inFlight,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await runner.RunAsync(wake, cancellationToken);
            outcomes.Add(outcome);
            GraphWorkerDispositionPolicy.Apply(outcome, logger);
        }
        finally
        {
            inFlight.TryRemove(wake.ThreadId, out _);
        }
    }
}

/// <summary>
///     Default park / complete / fail / cancel logging policy for the worker loop.
/// </summary>
file static class GraphWorkerDispositionPolicy
{
    public static void Apply(GraphThreadOutcome outcome, ILogger logger)
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
                // Default dead-letter: log + keep last-good checkpoint. Production: DLQ, alert, metrics.
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
