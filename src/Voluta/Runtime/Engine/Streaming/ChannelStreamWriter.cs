using System.Threading.Channels;
using Voluta.Abstractions.Streaming;

namespace Voluta.Runtime.Engine.Streaming;

/// <summary>
///     Forwards node-written custom / message events into a superstep channel for live stream merge.
/// </summary>
internal sealed class ChannelStreamWriter(
    ChannelWriter<StreamEvent> writer,
    string nodeName,
    long step) : IStreamWriter
{
    /// <inheritdoc />
    public ValueTask WriteCustomAsync(object? payload, CancellationToken cancellationToken = default)
    {
        return writer.WriteAsync(
            new StreamEvent
            {
                Mode = StreamMode.Events,
                Kind = StreamEventKind.Custom,
                Step = step,
                NodeNames = [nodeName],
                Payload = payload,
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask WriteMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        return writer.WriteAsync(
            new StreamEvent
            {
                Mode = StreamMode.Messages,
                Kind = StreamEventKind.Messages,
                Step = step,
                NodeNames = [nodeName],
                Payload = text,
            },
            cancellationToken);
    }
}
