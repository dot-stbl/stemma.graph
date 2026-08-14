using Voluta.Tools.Tools;

namespace Voluta.Tools.Mcp;

/// <summary>
///     Minimal MCP-shaped client: list tools and call a tool by name.
///     Implementations may target the demo HTTP surface (MockAdMcp) or a real MCP transport later.
/// </summary>
public interface IMcpClient
{
    /// <summary>
    ///     Lists tools advertised by the remote surface.
    /// </summary>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Tool catalog entries.</returns>
    public Task<IReadOnlyList<ToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calls a remote tool by name.
    /// </summary>
    /// <param name="call">Name + arguments.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Tool result (text + soft error flag).</returns>
    public Task<ToolResult> CallAsync(ToolCall call, CancellationToken cancellationToken = default);
}
