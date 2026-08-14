namespace Voluta.Agents.AI;

/// <summary>
///     Channel mapping for an <see cref="AgentGraphNode" />.
/// </summary>
public sealed class AgentNodeOptions
{
    /// <summary>
    ///     Channel to read the user message from (string). When null, uses
    ///     <see cref="Graph.GraphContext.TaskPayload" /> or an empty prompt.
    /// </summary>
    public string? InputChannel { get; init; }

    /// <summary>
    ///     Channel that receives the agent response text.
    /// </summary>
    public required string OutputChannel { get; init; }

}
