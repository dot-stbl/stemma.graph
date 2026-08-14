using Voluta.Abstractions.Diagnostics;

namespace Voluta.Exceptions.Run;

/// <summary>
///     Run exceeded the configured superstep recursion limit.
/// </summary>
/// <remarks>
///     Initializes an out-of-steps failure.
/// </remarks>
/// <param name="limit">Configured recursion limit.</param>
/// <param name="step">Superstep index that exceeded the limit.</param>
public sealed class GraphOutOfStepsException(int limit, long step) : GraphException(
    VolutaErrorCodes.GraphOutOfSteps,
    $"Run exceeded recursion limit of {limit} supersteps (at step {step}).")
{
    /// <summary>
    ///     Configured maximum superstep count.
    /// </summary>
    public int Limit { get; } = limit;

    /// <summary>
    ///     Superstep index when the limit was hit.
    /// </summary>
    public long Step { get; } = step;
}
