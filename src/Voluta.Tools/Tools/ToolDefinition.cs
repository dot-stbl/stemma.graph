namespace Voluta.Tools.Tools;

/// <summary>
///     Catalog entry for a tool (name, human description, optional input schema).
/// </summary>
/// <param name="name">Stable tool id used in <see cref="ToolCall.Name" />.</param>
/// <param name="description">Human-readable purpose for agent/tool catalogs.</param>
/// <param name="inputSchema">Optional JSON-Schema-shaped object (MCP-compatible).</param>
public sealed class ToolDefinition(string name, string? description = null, object? inputSchema = null)
{
    /// <summary>
    ///     Stable tool id.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    ///     Human-readable purpose.
    /// </summary>
    public string? Description { get; } = description;

    /// <summary>
    ///     Optional JSON Schema object describing arguments.
    /// </summary>
    public object? InputSchema { get; } = inputSchema;
}
