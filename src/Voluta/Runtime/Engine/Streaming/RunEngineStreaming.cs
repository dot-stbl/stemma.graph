using Voluta.Abstractions.Streaming;

namespace Voluta.Runtime.Engine.Streaming;

/// <summary>
///     Stream event emission helpers.
/// </summary>
internal static class RunEngineStreaming
{
    public static IEnumerable<StreamEvent> EmitCommit(
        StreamMode mode,
        long step,
        IReadOnlyList<string> nodeNames,
        IReadOnlyList<TaskChannelWrite> writes,
        ChannelStore store)
    {
        if (mode == StreamMode.Updates)
        {
            yield return new StreamEvent
            {
                Mode = StreamMode.Updates,
                Kind = StreamEventKind.Updates,
                Step = step,
                NodeNames = [.. nodeNames],
                Writes = [.. writes.Select(static item => item.Write)]
            };
        }
        else if (mode == StreamMode.Values)
        {
            yield return new StreamEvent
            {
                Mode = StreamMode.Values,
                Kind = StreamEventKind.Values,
                Step = step,
                NodeNames = [.. nodeNames],
                State = store.SnapshotValues()
            };
        }
    }

    public static StreamEvent Terminal(
        StreamMode mode,
        StreamEventKind kind,
        long step,
        ChannelStore store,
        object? payload = null)
    {
        return new StreamEvent
        {
            Mode = mode == StreamMode.Events ? StreamMode.Events : mode,
            Kind = kind,
            Step = step,
            State = mode == StreamMode.Values ? store.SnapshotValues() : null,
            Payload = payload
        };
    }
}
