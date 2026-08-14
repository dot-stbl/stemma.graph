using Voluta.Abstractions.Diagnostics;

namespace Voluta.Exceptions.Run;

/// <summary>
///     A requested history step is missing for a thread.
/// </summary>
/// <remarks>
///     Initializes a step-not-found failure.
/// </remarks>
/// <param name="message">Human-readable description.</param>
public sealed class GraphStepNotFoundException(string message)
    : GraphException(VolutaErrorCodes.GraphStepNotFound, message)
{
}
