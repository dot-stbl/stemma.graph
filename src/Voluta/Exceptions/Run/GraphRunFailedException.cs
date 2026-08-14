using Voluta.Abstractions.Diagnostics;

namespace Voluta.Exceptions.Run;

/// <summary>
///     Uncaught node exception or other superstep fault that failed the run.
/// </summary>
/// <remarks>
///     Initializes a failed-run exception.
/// </remarks>
/// <param name="message">Human-readable description.</param>
/// <param name="innerException">Node or apply failure.</param>
public sealed class GraphRunFailedException(string message, Exception? innerException = null)
    : GraphException(VolutaErrorCodes.GraphRunFailed, message, innerException)
{
}
