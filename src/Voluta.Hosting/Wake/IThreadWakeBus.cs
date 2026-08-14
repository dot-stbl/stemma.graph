namespace Voluta.Hosting.Wake;

/// <summary>
///     Wake bus: producers enqueue work for a thread; a worker drains wakes.
///     In-process default is <see cref="InMemoryThreadWakeBus" />; production
///     hosts swap in a durable queue (NATS, SQS, Service Bus) with the same shape.
/// </summary>
/// <remarks>
///     Multi-instance: the bus is a hint channel. The shared checkpointer is the
///     source of truth for thread state. Partition or lease by <c>threadId</c>
///     so two processes do not run the same thread concurrently.
/// </remarks>
public interface IThreadWakeBus
{
    /// <summary>
    ///     Enqueues a wake. Completes when the bus has accepted the item.
    /// </summary>
    /// <param name="wake">Start or resume signal for a thread.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask EnqueueAsync(ThreadWake wake, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Async stream of wakes until the bus is completed or cancellation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public IAsyncEnumerable<ThreadWake> ReadAllAsync(CancellationToken cancellationToken = default);
}
