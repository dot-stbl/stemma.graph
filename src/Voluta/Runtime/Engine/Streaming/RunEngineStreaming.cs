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
        ChannelStore store,
        IReadOnlyDictionary<string, object?>? postApplySnapshot = null)
    {
        if (mode == StreamMode.Updates)
        {
            yield return new StreamEvent
            {
                Mode = StreamMode.Updates,
                Kind = StreamEventKind.Updates,
                Step = step,
                NodeNames = nodeNames,
                Writes = [.. writes.Select(static item => item.Write)]
            };
        }
        else if (mode == StreamMode.Values)
        {
            // Shallow-copy when reusing post-apply snapshot so stream consumers cannot
            // mutate the dictionary owned by the in-memory checkpointer after Put.
            IReadOnlyDictionary<string, object?> state;
            if (postApplySnapshot is null)
            {
                state = store.SnapshotValues();
            }
            else
            {
                state = new Dictionary<string, object?>(postApplySnapshot, StringComparer.Ordinal);
            }

            yield return new StreamEvent
            {
                Mode = StreamMode.Values,
                Kind = StreamEventKind.Values,
                Step = step,
                NodeNames = nodeNames,
                State = state
            };
        }
    }

    public static StreamEvent Terminal(
        StreamMode mode,
        StreamEventKind kind,
        long step,
        ChannelStore store,
        object? payload = null,
        IReadOnlyDictionary<string, object?>? stateSnapshot = null)
    {
        return new StreamEvent
        {
            Mode = mode == StreamMode.Events ? StreamMode.Events : mode,
            Kind = kind,
            Step = step,
            State = mode == StreamMode.Values
                ? stateSnapshot ?? store.SnapshotValues()
                : null,
            Payload = payload
        };
    }
}
