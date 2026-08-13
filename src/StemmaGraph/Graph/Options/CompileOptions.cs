// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph;

/// <summary>
///     Options applied when compiling a <see cref="Graph.StateGraph" /> into a runnable graph.
/// </summary>
public sealed class CompileOptions
{
    /// <summary>
    ///     Maximum number of supersteps before the run fails with an out-of-steps error.
    /// </summary>
    public int RecursionLimit { get; init; } = 25;
}
