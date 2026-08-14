using System.Threading.Channels;

namespace Voluta.Samples.WorkerHost;

/// <summary>
///     In-process wake bus: producers enqueue work; the worker drains it.
///     Swap for a durable queue (NATS, SQS, Service Bus) in production — same shape.
/// </summary>
public sealed class ThreadWakeChannel
{
    private readonly Channel<ThreadWake> channel = Channel.CreateUnbounded<ThreadWake>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

    /// <summary>
    ///     Enqueues a wake. Completes when the item is accepted by the channel.
    /// </summary>
    public ValueTask EnqueueAsync(ThreadWake wake, CancellationToken cancellationToken = default)
    {
        return channel.Writer.WriteAsync(wake, cancellationToken);
    }

    /// <summary>
    ///     Async stream of wakes until the writer is completed or cancellation.
    /// </summary>
    public IAsyncEnumerable<ThreadWake> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    /// <summary>
    ///     Signals no further wakes (host shutdown after demo).
    /// </summary>
    public void Complete()
    {
        channel.Writer.TryComplete();
    }
}
