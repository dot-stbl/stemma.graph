using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Abstractions.Results;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Abstractions.Streaming;
using StemmaGraph.Graph.Options;

namespace StemmaGraph.Graph;

/// <summary>
///     Helpers to treat a <see cref="CompiledGraph" /> as a parent-graph node.
/// </summary>
public static class Subgraph
{
    /// <summary>
    ///     Builds a node handler that runs <paramref name="child" /> to a terminal status
    ///     and maps selected child channel values into parent writes.
    /// </summary>
    /// <param name="child">Compiled child graph.</param>
    /// <param name="inputChannels">Parent channel names seeded into the child as input writes.</param>
    /// <param name="outputChannels">Child channel names copied back as parent writes.</param>
    /// <param name="threadIdFactory">
    ///     Optional factory for child thread ids (default: parent node name + random).
    /// </param>
    /// <returns>A <see cref="NodeHandler" /> suitable for <c>StateGraph.AddNode</c>.</returns>
    public static NodeHandler AsNode(
        CompiledGraph child,
        IReadOnlyList<string>? inputChannels = null,
        IReadOnlyList<string>? outputChannels = null,
        Func<GraphContext, string>? threadIdFactory = null)
    {
        inputChannels ??= [];
        outputChannels ??= [];
        threadIdFactory ??= static context => $"{context.NodeName}-{Guid.NewGuid():N}";

        return async (context, cancellationToken) =>
        {
            var inputs = new List<ChannelWrite>();
            foreach (var channelName in inputChannels)
            {
                inputs.Add(new ChannelWrite(channelName, context.Read<object>(channelName)));
            }

            if (context.TaskPayload is IEnumerable<ChannelWrite> sendWrites)
            {
                inputs.AddRange(sendWrites);
            }

            var threadId = threadIdFactory(context);
            var terminal = await child.InvokeAsync(
                inputs,
                new RunOptions { ThreadId = threadId, StreamMode = StreamMode.Values },
                cancellationToken);

            if (terminal.Kind == StreamEventKind.Interrupt)
            {
                return NodeResult.Interrupt(terminal.Payload);
            }

            if (terminal.Kind is StreamEventKind.Failed or StreamEventKind.Cancelled)
            {
                throw new InvalidOperationException(
                    $"Subgraph '{threadId}' ended with {terminal.Kind}.");
            }

            var writes = new List<ChannelWrite>();
            if (terminal.State is not null)
            {
                foreach (var channelName in outputChannels)
                {
                    if (terminal.State.TryGetValue(channelName, out var value))
                    {
                        writes.Add(new ChannelWrite(channelName, value));
                    }
                }
            }

            return NodeResult.Continue(writes);
        };
    }
}
