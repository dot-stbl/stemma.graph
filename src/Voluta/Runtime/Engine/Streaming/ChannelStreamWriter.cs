using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Voluta.Abstractions.Streaming;
using Voluta.Diagnostics;

namespace Voluta.Runtime.Engine.Streaming;

/// <summary>
///     Forwards node-written custom / message events into a superstep channel for live stream merge.
///     Uses TryWrite; drops when the bounded live buffer is full and increments
///     <see cref="VolutaDiagnostics.StreamDropped" />.
/// </summary>
internal sealed class ChannelStreamWriter(
    ChannelWriter<StreamEvent> writer,
    string nodeName,
    long step) : IStreamWriter
{
    /// <inheritdoc />
    public ValueTask WriteCustomAsync(object? payload, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var item = new StreamEvent
        {
            Mode = StreamMode.Events,
            Kind = StreamEventKind.Custom,
            Step = step,
            NodeNames = [nodeName],
            Payload = payload,
        };
        TryWriteOrDrop(item, "custom");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask WriteMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var item = new StreamEvent
        {
            Mode = StreamMode.Messages,
            Kind = StreamEventKind.Messages,
            Step = step,
            NodeNames = [nodeName],
            Payload = text,
        };
        TryWriteOrDrop(item, "messages");
        return ValueTask.CompletedTask;
    }

    private void TryWriteOrDrop(StreamEvent item, string streamKind)
    {
        if (writer.TryWrite(item))
        {
            return;
        }

        VolutaDiagnostics.StreamDropped.Add(
            1,
            new KeyValuePair<string, object?>(VolutaDiagnostics.TagNodeName, nodeName),
            new KeyValuePair<string, object?>(VolutaDiagnostics.TagStreamKind, streamKind));
    }
}
