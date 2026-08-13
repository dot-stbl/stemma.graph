namespace Voluta.Exceptions;

/// <summary>
///     Topology validation failure raised during <see cref="Graph.Builder.StateGraph.Compile" />.
/// </summary>
/// <remarks>
///     Initializes a compile-time graph exception.
/// </remarks>
/// <param name="code">Stable error code.</param>
/// <param name="message">Validation message.</param>
public sealed class GraphCompileException(string code, string message) : GraphException(code, message)
{
}
