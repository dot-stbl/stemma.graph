namespace Voluta.Tools.Tools;

/// <summary>
///     One tool invocation request (name + arguments map).
/// </summary>
/// <param name="name">Tool id matching <see cref="ToolDefinition.Name" />.</param>
/// <param name="arguments">JSON-serializable argument bag (empty when none).</param>
public sealed class ToolCall(string name, IReadOnlyDictionary<string, object?>? arguments = null)
{
    /// <summary>
    ///     Tool id.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    ///     Argument bag (never null; empty when omitted).
    /// </summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; } =
        arguments ?? new Dictionary<string, object?>(StringComparer.Ordinal);
}
