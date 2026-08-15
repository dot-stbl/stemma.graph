using Voluta.Abstractions.Diagnostics;

namespace Voluta.Exceptions.Run;

/// <summary>
///     A thread id has no checkpoint in the checkpointer.
/// </summary>
/// <remarks>
///     Initializes a thread-not-found failure.
/// </remarks>
/// <param name="message">Human-readable description.</param>
public sealed class GraphThreadNotFoundException(string message)
    : GraphException(VolutaErrorCodes.GraphThreadNotFound, message)
{
}
