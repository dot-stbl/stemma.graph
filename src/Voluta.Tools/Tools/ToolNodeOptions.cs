namespace Voluta.Tools.Tools;

/// <summary>
///     Channel mapping and timeout for a <see cref="ToolGraphNode" />.
/// </summary>
public sealed class ToolNodeOptions
{
    /// <summary>
    ///     Channel that receives the tool result text.
    /// </summary>
    public required string OutputChannel { get; init; }

    /// <summary>
    ///     Optional channel that receives a boolean error flag after soft failures.
    /// </summary>
    public string? ErrorChannel { get; init; }

    /// <summary>
    ///     Optional wall-clock timeout for the tool body. Linked with the graph token.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    ///     When true, soft tool errors (<see cref="ToolResult.IsError" />) throw
    ///     <see cref="ToolInvocationException" /> instead of writing an error result.
    /// </summary>
    public bool ThrowOnError { get; init; }
}
