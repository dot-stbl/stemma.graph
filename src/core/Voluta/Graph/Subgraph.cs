using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Graph.Options;

namespace Voluta.Graph;

/// <summary>
///     Helpers to treat a <see cref="CompiledGraph" /> as a parent-graph node.
/// </summary>
public static class Subgraph
{
    /// <summary>
    ///     Builds a node handler that runs <paramref name="child" /> to a terminal status
    ///     and maps selected child channel values into parent writes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Nested checkpoints use a dedicated child thread id. Default factory is
    ///         <c>{parentThreadId}/{nodeName}</c> (stable across parent resume). Pass
    ///         <paramref name="threadIdFactory" /> for multi-agent nests that need a custom
    ///         namespace (e.g. per-tenant or per-tool-call suffix).
    ///     </para>
    ///     <para>
    ///         When the child interrupts, the parent node returns
    ///         <see cref="NodeResult.Interrupt" /> with the child payload so the parent
    ///         checkpoint keeps <c>NextNodes = [subgraph node]</c>. On parent resume, the
    ///         same node re-runs and calls <see cref="CompiledGraph.ResumeInvokeAsync" />
    ///         on the child thread with the host <see cref="Command" /> (or
    ///         <see cref="Command.Approve" /> wrapping a non-command resume payload).
    ///     </para>
    /// </remarks>
    /// <param name="child">Compiled child graph.</param>
    /// <param name="inputChannels">Parent channel names seeded into the child as input writes.</param>
    /// <param name="outputChannels">Child channel names copied back as parent writes.</param>
    /// <param name="threadIdFactory">
    ///     Optional factory for child thread ids. Default: stable
    ///     <c>{context.ThreadId}/{context.NodeName}</c>, falling back to
    ///     <c>{context.NodeName}</c> when the parent thread id is unavailable.
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
        threadIdFactory ??= static context => SubgraphThreadIds.Default(context);

        return async (context, cancellationToken) =>
        {
            var threadId = threadIdFactory(context);
            var terminal = context.ResumePayload is null
                ? await child.InvokeAsync(
                    SubgraphInputs.Collect(context, inputChannels),
                    new RunOptions { ThreadId = threadId, StreamMode = StreamMode.Values },
                    cancellationToken)
                : await SubgraphResume.DrainAsync(
                    child,
                    threadId,
                    SubgraphResume.ToCommand(context.ResumePayload),
                    cancellationToken);

            return terminal.Kind switch
            {
                StreamEventKind.Interrupt => NodeResult.Interrupt(terminal.Payload),
                StreamEventKind.Failed or StreamEventKind.Cancelled =>
                    throw new InvalidOperationException(
                        $"Subgraph '{threadId}' ended with {terminal.Kind}."),
                _ => NodeResult.Continue(SubgraphOutputs.Map(terminal, outputChannels)),
            };
        };
    }
}

/// <summary>
///     Default nested thread id namespace for <see cref="Subgraph.AsNode" />.
/// </summary>
file static class SubgraphThreadIds
{
    public static string Default(GraphContext context)
    {
        return string.IsNullOrWhiteSpace(context.ThreadId)
            ? context.NodeName
            : $"{context.ThreadId}/{context.NodeName}";
    }
}

/// <summary>
///     Parent → child input channel collection.
/// </summary>
file static class SubgraphInputs
{
    public static List<ChannelWrite> Collect(
        GraphContext context,
        IReadOnlyList<string> inputChannels)
    {
        var inputs = new List<ChannelWrite>(inputChannels.Count);
        foreach (var channelName in inputChannels)
        {
            inputs.Add(new ChannelWrite(channelName, context.Read<object>(channelName)));
        }

        if (context.TaskPayload is IEnumerable<ChannelWrite> sendWrites)
        {
            inputs.AddRange(sendWrites);
        }

        return inputs;
    }
}

/// <summary>
///     Child terminal state → parent writes.
/// </summary>
file static class SubgraphOutputs
{
    public static List<ChannelWrite> Map(StreamEvent terminal, IReadOnlyList<string> outputChannels)
    {
        var writes = new List<ChannelWrite>();
        if (terminal.State is null)
        {
            return writes;
        }

        foreach (var channelName in outputChannels)
        {
            if (terminal.State.TryGetValue(channelName, out var value))
            {
                writes.Add(new ChannelWrite(channelName, value));
            }
        }

        return writes;
    }
}

/// <summary>
///     Maps parent resume payload into a child <see cref="Command" /> and drains
///     resume streams with <see cref="StreamMode.Values" /> so output mapping sees state.
/// </summary>
file static class SubgraphResume
{
    public static Command ToCommand(object? resumePayload)
    {
        return resumePayload is Command command
            ? command
            : Command.Approve(resumePayload);
    }

    public static async Task<StreamEvent> DrainAsync(
        CompiledGraph child,
        string threadId,
        Command command,
        CancellationToken cancellationToken)
    {
        StreamEvent? last = null;
        await foreach (var item in child.ResumeAsync(
                           threadId,
                           command,
                           StreamMode.Values,
                           cancellationToken))
        {
            last = item;
            if (item.Kind is StreamEventKind.Failed && item.Payload is Exception exception)
            {
                throw exception;
            }

            if (item.Kind is StreamEventKind.End
                or StreamEventKind.Interrupt
                or StreamEventKind.Failed
                or StreamEventKind.Cancelled)
            {
                return item;
            }
        }

        return last ?? new StreamEvent
        {
            Mode = StreamMode.Values,
            Kind = StreamEventKind.End,
        };
    }
}
