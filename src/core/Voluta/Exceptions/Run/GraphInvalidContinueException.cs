using Voluta.Abstractions.Diagnostics;

namespace Voluta.Exceptions.Run;

/// <summary>
///     Continue was requested for a thread that is not in Running status.
/// </summary>
/// <remarks>
///     Initializes an invalid-continue failure.
/// </remarks>
/// <param name="message">Human-readable description.</param>
public sealed class GraphInvalidContinueException(string message)
    : GraphException(VolutaErrorCodes.GraphInvalidContinue, message)
{
}
