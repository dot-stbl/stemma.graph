using System.Threading.Channels;

namespace Voluta.Hosting.Wake;

/// <summary>
///     In-process wake bus backed by an unbounded <see cref="Channel{T}" />.
///     Suitable for single-process hosts and samples; swap for a durable queue
///     in multi-instance production.
/// </summary>
public sealed class InMemoryThreadWakeBus : IThreadWakeBus
{
    private readonly Channel<ThreadWake> channel = Channel.CreateUnbounded<ThreadWake>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

    /// <inheritdoc />
    public ValueTask EnqueueAsync(ThreadWake wake, CancellationToken cancellationToken = default)
    {
        return channel.Writer.WriteAsync(wake, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ThreadWake> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    /// <summary>
    ///     Signals no further wakes (host shutdown after a finite demo or drain).
    ///     Durable bus implementations typically do not need an equivalent.
    /// </summary>
    public void Complete()
    {
        channel.Writer.TryComplete();
    }
}
