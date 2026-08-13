namespace StemmaGraph.Abstractions.Runtime;

/// <summary>
///     Dynamic PUSH task: invoke <see cref="Node" /> once with <see cref="Payload" /> next superstep.
/// </summary>
public sealed class Send
{
    /// <summary>
    ///     Initializes a send targeting a node with an optional payload.
    /// </summary>
    /// <param name="node">Target node name (must exist on the compiled graph).</param>
    /// <param name="payload">Task-local payload exposed as <c>GraphContext.TaskPayload</c>.</param>
    public Send(string node, object? payload = null)
    {
        if (string.IsNullOrWhiteSpace(node))
        {
            throw new ArgumentException("Send node name must be non-empty.", nameof(node));
        }

        Node = node;
        Payload = payload;
    }

    /// <summary>
    ///     Target node name.
    /// </summary>
    public string Node { get; }

    /// <summary>
    ///     Payload for the scheduled task (not a channel write).
    /// </summary>
    public object? Payload { get; }
}
