using Microsoft.Agents.AI;
using Voluta.Graph.Builder;

namespace Voluta.Agents.AI;

/// <summary>
///     Instance-style registration helpers for MAF agents (not extension methods).
/// </summary>
public static class AgentNodes
{
    /// <summary>
    ///     Adds an <see cref="AgentGraphNode" /> under <paramref name="name" />.
    /// </summary>
    /// <param name="graph">Graph builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="agent">MAF agent.</param>
    /// <param name="outputChannel">Channel for agent text.</param>
    /// <param name="inputChannel">Optional input channel.</param>
    /// <returns>The same <paramref name="graph" /> for chaining.</returns>
    public static StateGraph Add(
        StateGraph graph,
        string name,
        AIAgent agent,
        string outputChannel,
        string? inputChannel = null)
    {
        return graph.AddNode(name, AgentGraphNode.Create(agent, outputChannel, inputChannel));
    }
}
