// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Runtime.Exceptions;

/// <summary>
/// Run exceeded the configured superstep recursion limit.
/// </summary>
public sealed class GraphOutOfStepsException : GraphException
{
    /// <summary>
    /// Initializes an out-of-steps failure.
    /// </summary>
    /// <param name="limit">Configured recursion limit.</param>
    /// <param name="step">Superstep index that exceeded the limit.</param>
    public GraphOutOfStepsException(int limit, long step)
        : base(
            "graph.out_of_steps",
            $"Run exceeded recursion limit of {limit} supersteps (at step {step}).")
    {
        Limit = limit;
        Step = step;
    }

    /// <summary>
    /// Configured maximum superstep count.
    /// </summary>
    public int Limit { get; }

    /// <summary>
    /// Superstep index when the limit was hit.
    /// </summary>
    public long Step { get; }
}
