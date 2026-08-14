using Voluta.Tools.Tools;

namespace Voluta.Tools.Mcp;

/// <summary>
///     <see cref="ITool" /> that forwards invocations to an <see cref="IMcpClient" />.
/// </summary>
public sealed class McpTool : ITool
{
    private readonly IMcpClient client;

    /// <summary>
    ///     Creates an MCP-backed tool with a known definition.
    /// </summary>
    /// <param name="client">MCP client.</param>
    /// <param name="definition">Remote tool metadata.</param>
    public McpTool(IMcpClient client, ToolDefinition definition)
    {
        this.client = client;
        Definition = definition;
    }

    /// <summary>
    ///     Creates an MCP-backed tool with a known definition.
    /// </summary>
    /// <param name="client">MCP client.</param>
    /// <param name="name">Remote tool name.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>Tool ready for <see cref="ToolGraphNode" />.</returns>
    public static McpTool Create(IMcpClient client, string name, string? description = null)
    {
        return new McpTool(client, new ToolDefinition(name, description));
    }

    /// <inheritdoc />
    public ToolDefinition Definition { get; }

    /// <inheritdoc />
    public Task<ToolResult> InvokeAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        var effective = string.Equals(call.Name, Definition.Name, StringComparison.Ordinal)
            ? call
            : new ToolCall(Definition.Name, call.Arguments);
        return client.CallAsync(effective, cancellationToken);
    }
}
