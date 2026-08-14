namespace Voluta.Tools.Tools;

/// <summary>
///     A named tool the graph can invoke (delegate, MCP remote, or custom).
/// </summary>
public interface ITool
{
    /// <summary>
    ///     Stable tool metadata (name, description, optional JSON Schema).
    /// </summary>
    public ToolDefinition Definition { get; }

    /// <summary>
    ///     Invokes the tool for one call.
    /// </summary>
    /// <param name="call">Tool name + arguments.</param>
    /// <param name="cancellationToken">Cooperative cancellation (and timeout token when linked).</param>
    /// <returns>Structured tool result (text + error flag).</returns>
    public Task<ToolResult> InvokeAsync(ToolCall call, CancellationToken cancellationToken = default);
}
