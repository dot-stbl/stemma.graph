namespace StemmaGraph.Exceptions.Run;

/// <summary>
///     Resume was requested for a thread that is not interrupted.
/// </summary>
/// <remarks>
///     Initializes an invalid-resume failure.
/// </remarks>
/// <param name="message">Human-readable description.</param>
public sealed class GraphInvalidResumeException(string message) : GraphException("graph.invalid_resume", message)
{
}
