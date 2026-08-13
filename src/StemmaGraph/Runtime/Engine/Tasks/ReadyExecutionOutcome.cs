using StemmaGraph.Exceptions;

namespace StemmaGraph.Runtime.Engine.Tasks;

/// <summary>
///     Outcome of attempting to execute the ready set.
/// </summary>
internal sealed class ReadyExecutionOutcome
{
    public bool Cancelled { get; init; }

    public Exception? Exception { get; init; }

    public GraphException? Failure { get; init; }

    public IReadOnlyList<NodeExecution>? Executions { get; init; }
}
