namespace Voluta.Exceptions.Run;

/// <summary>
///     Resume <see cref="Abstractions.Runtime.Command"/> failed taxonomy validation
///     (unknown kind, empty kind, or kind-specific payload/values rules).
/// </summary>
/// <remarks>
///     Initializes an invalid-command failure. Stable code: <c>hitl.invalid_command</c>.
/// </remarks>
/// <param name="message">Safe human-readable description (no PII/secrets).</param>
public sealed class GraphInvalidCommandException(string message)
    : GraphException("hitl.invalid_command", message)
{
}
