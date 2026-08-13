namespace Voluta.Exceptions;

/// <summary>
///     LastValue multi-writer violation within a single superstep.
/// </summary>
/// <remarks>
///     Initializes a concurrent-update failure.
/// </remarks>
/// <param name="message">Human-readable description.</param>
public sealed class GraphConcurrentUpdateException(string message)
    : GraphException("channel.concurrent_update", message)
{
}
