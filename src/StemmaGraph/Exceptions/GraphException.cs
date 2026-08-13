// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Runtime.Exceptions;

/// <summary>
///     Base type for graph runtime and compile failures with a stable machine code.
/// </summary>
/// <remarks>
///     Initializes a graph exception.
/// </remarks>
/// <param name="code">Stable dot.case error code for host branching.</param>
/// <param name="message">Safe human message.</param>
/// <param name="innerException">Optional inner exception.</param>
public class GraphException(string code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    /// <summary>
    ///     Stable machine-readable error code (for example <c>graph.out_of_steps</c>).
    /// </summary>
    public string Code { get; } = code;
}
