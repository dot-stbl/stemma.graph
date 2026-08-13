// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Runtime.Exceptions;

/// <summary>
/// Base type for graph runtime and compile failures with a stable machine code.
/// </summary>
public class GraphException : Exception
{
    /// <summary>
    /// Initializes a graph exception.
    /// </summary>
    /// <param name="code">Stable dot.case error code for host branching.</param>
    /// <param name="message">Safe human message.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public GraphException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>
    /// Stable machine-readable error code (for example <c>graph.out_of_steps</c>).
    /// </summary>
    public string Code { get; }
}
