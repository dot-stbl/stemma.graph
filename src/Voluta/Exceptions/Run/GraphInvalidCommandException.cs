using Voluta.Abstractions.Diagnostics;

namespace Voluta.Exceptions.Run;

/// <summary>
///     Resume <see cref="Abstractions.Runtime.Command"/> failed taxonomy validation
///     (unknown kind, empty kind, or kind-specific payload/values rules).
/// </summary>
/// <remarks>
///     Initializes an invalid-command failure. Stable codes:
///     <see cref="VolutaErrorCodes.CommandInvalidKind"/> or
///     <see cref="VolutaErrorCodes.CommandInvalidPayload"/>.
/// </remarks>
/// <param name="code">Stable error code from the command catalog.</param>
/// <param name="message">Safe human-readable description (no PII/secrets).</param>
public sealed class GraphInvalidCommandException(string code, string message)
    : GraphException(code, message)
{
}
