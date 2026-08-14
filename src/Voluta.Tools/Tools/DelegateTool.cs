namespace Voluta.Tools.Tools;

/// <summary>
///     <see cref="ITool" /> backed by an async delegate (unit tests and local tools).
/// </summary>
public sealed class DelegateTool(
    ToolDefinition definition,
    Func<ToolCall, CancellationToken, Task<ToolResult>> invoke) : ITool
{
    /// <summary>
    ///     Creates a tool with a name and async handler.
    /// </summary>
    /// <param name="name">Tool id.</param>
    /// <param name="invoke">Handler receiving call + token.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>Ready-to-register tool.</returns>
    public static DelegateTool Create(
        string name,
        Func<ToolCall, CancellationToken, Task<ToolResult>> invoke,
        string? description = null)
    {
        return new DelegateTool(new ToolDefinition(name, description), invoke);
    }

    /// <inheritdoc />
    public ToolDefinition Definition { get; } = definition;

    /// <inheritdoc />
    public Task<ToolResult> InvokeAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        return invoke(call, cancellationToken);
    }
}
