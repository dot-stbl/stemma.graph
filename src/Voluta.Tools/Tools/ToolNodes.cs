using Voluta.Graph;
using Voluta.Graph.Builder;

namespace Voluta.Tools.Tools;

/// <summary>
///     Instance-style registration helpers for tool nodes (not extension methods).
/// </summary>
public static class ToolNodes
{
    /// <summary>
    ///     Adds a <see cref="ToolGraphNode" /> under <paramref name="name" /> with static arguments.
    /// </summary>
    /// <param name="graph">Graph builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="tool">Tool to invoke.</param>
    /// <param name="outputChannel">Channel for result text.</param>
    /// <param name="arguments">Optional static arguments.</param>
    /// <param name="timeout">Optional timeout.</param>
    /// <returns>The same <paramref name="graph" /> for chaining.</returns>
    public static StateGraph Add(
        StateGraph graph,
        string name,
        ITool tool,
        string outputChannel,
        IReadOnlyDictionary<string, object?>? arguments,
        TimeSpan? timeout)
    {
        return graph.AddNode(name, ToolGraphNode.Create(tool, outputChannel, arguments, timeout));
    }

    /// <summary>
    ///     Adds a <see cref="ToolGraphNode" /> under <paramref name="name" /> (no static args).
    /// </summary>
    /// <param name="graph">Graph builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="tool">Tool to invoke.</param>
    /// <param name="outputChannel">Channel for result text.</param>
    /// <returns>The same <paramref name="graph" /> for chaining.</returns>
    public static StateGraph Add(
        StateGraph graph,
        string name,
        ITool tool,
        string outputChannel)
    {
        return Add(graph, name, tool, outputChannel, arguments: null, timeout: null);
    }

    /// <summary>
    ///     Adds a <see cref="ToolGraphNode" /> that builds the call from context.
    /// </summary>
    /// <param name="graph">Graph builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="tool">Tool to invoke.</param>
    /// <param name="outputChannel">Channel for result text.</param>
    /// <param name="callFactory">Builds the call from frozen context.</param>
    /// <param name="timeout">Optional timeout.</param>
    /// <returns>The same <paramref name="graph" /> for chaining.</returns>
    public static StateGraph Add(
        StateGraph graph,
        string name,
        ITool tool,
        string outputChannel,
        Func<GraphContext, ToolCall> callFactory,
        TimeSpan? timeout)
    {
        return graph.AddNode(name, ToolGraphNode.Create(tool, outputChannel, callFactory, timeout));
    }

    /// <summary>
    ///     Adds a <see cref="ToolGraphNode" /> that builds the call from context (no timeout).
    /// </summary>
    /// <param name="graph">Graph builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="tool">Tool to invoke.</param>
    /// <param name="outputChannel">Channel for result text.</param>
    /// <param name="callFactory">Builds the call from frozen context.</param>
    /// <returns>The same <paramref name="graph" /> for chaining.</returns>
    public static StateGraph Add(
        StateGraph graph,
        string name,
        ITool tool,
        string outputChannel,
        Func<GraphContext, ToolCall> callFactory)
    {
        return Add(graph, name, tool, outputChannel, callFactory, timeout: null);
    }
}
