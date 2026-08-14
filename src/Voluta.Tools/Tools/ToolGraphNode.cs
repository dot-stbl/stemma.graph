using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Graph;
using Voluta.Graph.Builder;

namespace Voluta.Tools.Tools;

/// <summary>
///     Invokes an <see cref="ITool" /> and writes the result text to a channel.
///     Register with <see cref="StateGraph.AddNode(string, IGraphNode)" />.
/// </summary>
public sealed class ToolGraphNode(ITool tool, ToolNodeOptions options, Func<GraphContext, ToolCall>? callFactory = null)
    : IGraphNode
{
    /// <summary>
    ///     Creates a node that always calls <paramref name="tool" /> with a fixed name and optional static args.
    /// </summary>
    /// <param name="tool">Tool implementation.</param>
    /// <param name="outputChannel">Channel for result text.</param>
    /// <param name="arguments">Optional static arguments.</param>
    /// <param name="timeout">Optional timeout.</param>
    /// <returns>Ready-to-register graph node.</returns>
    public static ToolGraphNode Create(
        ITool tool,
        string outputChannel,
        IReadOnlyDictionary<string, object?>? arguments,
        TimeSpan? timeout)
    {
        return new ToolGraphNode(
            tool,
            new ToolNodeOptions
            {
                OutputChannel = outputChannel,
                Timeout = timeout,
            },
            _ => new ToolCall(tool.Definition.Name, arguments));
    }

    /// <summary>
    ///     Creates a node that always calls <paramref name="tool" /> with a fixed name (no static args).
    /// </summary>
    /// <param name="tool">Tool implementation.</param>
    /// <param name="outputChannel">Channel for result text.</param>
    /// <returns>Ready-to-register graph node.</returns>
    public static ToolGraphNode Create(ITool tool, string outputChannel)
    {
        return Create(tool, outputChannel, arguments: null, timeout: null);
    }

    /// <summary>
    ///     Creates a node that builds the <see cref="ToolCall" /> from graph state each invocation.
    /// </summary>
    /// <param name="tool">Tool implementation.</param>
    /// <param name="outputChannel">Channel for result text.</param>
    /// <param name="callFactory">Builds the call from frozen context.</param>
    /// <param name="timeout">Optional timeout.</param>
    /// <returns>Ready-to-register graph node.</returns>
    public static ToolGraphNode Create(
        ITool tool,
        string outputChannel,
        Func<GraphContext, ToolCall> callFactory,
        TimeSpan? timeout)
    {
        return new ToolGraphNode(
            tool,
            new ToolNodeOptions
            {
                OutputChannel = outputChannel,
                Timeout = timeout,
            },
            callFactory);
    }

    /// <summary>
    ///     Creates a node that builds the <see cref="ToolCall" /> from graph state (no timeout).
    /// </summary>
    /// <param name="tool">Tool implementation.</param>
    /// <param name="outputChannel">Channel for result text.</param>
    /// <param name="callFactory">Builds the call from frozen context.</param>
    /// <returns>Ready-to-register graph node.</returns>
    public static ToolGraphNode Create(
        ITool tool,
        string outputChannel,
        Func<GraphContext, ToolCall> callFactory)
    {
        return Create(tool, outputChannel, callFactory, timeout: null);
    }

    /// <inheritdoc />
    public async Task<NodeResult> InvokeAsync(GraphContext context, CancellationToken cancellationToken = default)
    {
        var call = ResolveCall(context);
        using var timeoutSource = options.Timeout is { } timeout
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;
        if (timeoutSource is not null && options.Timeout is { } timeoutValue)
        {
            timeoutSource.CancelAfter(timeoutValue);
        }

        var effectiveToken = timeoutSource?.Token ?? cancellationToken;
        ToolResult result;
        try
        {
            result = await tool.InvokeAsync(call, effectiveToken);
        }
        catch (OperationCanceledException exception) when (
            timeoutSource is not null
            && timeoutSource.IsCancellationRequested
            && !cancellationToken.IsCancellationRequested)
        {
            throw new ToolInvocationException(
                call.Name,
                $"tool '{call.Name}' timed out after {options.Timeout}",
                exception);
        }

        if (result.IsError && options.ThrowOnError)
        {
            throw new ToolInvocationException(call.Name, result.Text);
        }

        var writes = new List<ChannelWrite>
        {
            new(options.OutputChannel, result.Text),
        };
        if (options.ErrorChannel is { Length: > 0 } errorChannel)
        {
            writes.Add(new ChannelWrite(errorChannel, result.IsError));
        }

        return NodeResult.Continue(writes);
    }

    private ToolCall ResolveCall(GraphContext context)
    {
        return callFactory is not null
            ? callFactory(context)
            : context.TaskPayload switch
            {
                ToolCall payloadCall => payloadCall,
                string name when name.Length > 0 => new ToolCall(name),
                _ => new ToolCall(tool.Definition.Name),
            };
    }
}
