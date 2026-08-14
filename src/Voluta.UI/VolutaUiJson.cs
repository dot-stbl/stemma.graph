using System.Text.Json;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Streaming;
using Voluta.Abstractions.Topology;

namespace Voluta.UI;

/// <summary>
///     Wire-safe projections for UI JSON (avoids dumping non-serializable payloads raw).
/// </summary>
internal static class VolutaUiJson
{
    public static object ToWire(StreamEvent item)
    {
        return new
        {
            mode = item.Mode.ToString(),
            kind = item.Kind.ToString(),
            step = item.Step,
            nodeNames = item.NodeNames,
            writes = item.Writes.Select(static write => new
            {
                channelName = write.ChannelName,
                value = FormatValue(write.Value),
            }),
            state = item.State?.ToDictionary(
                static pair => pair.Key,
                static pair => FormatValue(pair.Value),
                StringComparer.Ordinal),
            payload = FormatValue(item.Payload),
        };
    }

    public static object ToWire(CheckpointSnapshot snapshot)
    {
        return new
        {
            formatVersion = snapshot.FormatVersion,
            threadId = snapshot.ThreadId,
            step = snapshot.Step,
            status = snapshot.Status.ToString(),
            lastNode = snapshot.LastNode,
            nextNodes = snapshot.NextNodes,
            interruptPayload = FormatValue(snapshot.InterruptPayload),
            channelValues = snapshot.ChannelValues.ToDictionary(
                static pair => pair.Key,
                static pair => FormatValue(pair.Value),
                StringComparer.Ordinal),
            channelVersions = snapshot.ChannelVersions,
            versionsSeen = snapshot.VersionsSeen,
            pendingWrites = snapshot.PendingWrites.Select(static write => new
            {
                taskId = write.TaskId,
                channelName = write.ChannelName,
                value = FormatValue(write.Value),
            }),
            pendingSends = snapshot.PendingSends.Select(static send => new
            {
                nodeName = send.NodeName,
                taskId = send.TaskId,
                payload = FormatValue(send.Payload),
            }),
        };
    }

    public static object ToWire(ThreadSnapshot state)
    {
        return new
        {
            threadId = state.ThreadId,
            step = state.Step,
            status = state.Status.ToString(),
            lastNode = state.LastNode,
            nextNodes = state.NextNodes,
            interruptPayload = FormatValue(state.InterruptPayload),
            values = state.Values.ToDictionary(
                static pair => pair.Key,
                static pair => FormatValue(pair.Value),
                StringComparer.Ordinal),
        };
    }

    public static object ToWire(GraphDescription topology)
    {
        return new
        {
            nodes = topology.Nodes,
            channels = topology.Channels.ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value.ToString(),
                StringComparer.Ordinal),
            staticEdges = topology.StaticEdges.Select(static edge => new
            {
                source = edge.Source,
                target = edge.Target,
            }),
            conditionalSources = topology.ConditionalSources,
            recursionLimit = topology.RecursionLimit,
        };
    }

    public static object ToWireTerminal(StreamEvent terminal)
    {
        return new
        {
            kind = terminal.Kind.ToString(),
            step = terminal.Step,
            payload = FormatValue(terminal.Payload),
            nodeNames = terminal.NodeNames,
        };
    }

    public static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string text => text,
            JsonElement element => element.ToString(),
            System.Collections.IEnumerable enumerable and not string =>
                "[" + string.Join(
                    ", ",
                    enumerable.Cast<object?>().Select(static item => FormatValue(item))) + "]",
            _ => value.ToString() ?? "null",
        };
    }
}
