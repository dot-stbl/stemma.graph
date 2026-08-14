namespace Voluta.Tools.Tools;

/// <summary>
///     Outcome of a tool invocation (text payload + error flag).
/// </summary>
/// <param name="text">Primary text content (JSON or plain).</param>
/// <param name="isError">When true, the tool reported failure without throwing.</param>
public sealed class ToolResult(string text, bool isError = false)
{
    /// <summary>
    ///     Primary text content.
    /// </summary>
    public string Text { get; } = text;

    /// <summary>
    ///     Soft failure flag (MCP <c>isError</c> style).
    /// </summary>
    public bool IsError { get; } = isError;

    /// <summary>
    ///     Successful result with text.
    /// </summary>
    /// <param name="text">Payload text.</param>
    /// <returns>Non-error result.</returns>
    public static ToolResult Ok(string text)
    {
        return new ToolResult(text, isError: false);
    }

    /// <summary>
    ///     Soft error result (does not throw).
    /// </summary>
    /// <param name="message">Error message text.</param>
    /// <returns>Error-flagged result.</returns>
    public static ToolResult Error(string message)
    {
        return new ToolResult(message, isError: true);
    }
}
