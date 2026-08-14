namespace Voluta.Tools.Tools;

/// <summary>
///     Thrown when a tool times out, soft-fails with <see cref="ToolNodeOptions.ThrowOnError" />,
///     or the call cannot be resolved.
/// </summary>
public sealed class ToolInvocationException : Exception
{
    /// <summary>
    ///     Creates a tool invocation failure.
    /// </summary>
    /// <param name="toolName">Tool id.</param>
    /// <param name="message">Failure detail.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public ToolInvocationException(string toolName, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ToolName = toolName;
    }

    /// <summary>
    ///     Tool id that failed.
    /// </summary>
    public string ToolName { get; }
}
