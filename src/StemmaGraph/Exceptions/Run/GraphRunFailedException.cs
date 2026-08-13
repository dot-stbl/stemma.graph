// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Runtime.Exceptions;

/// <summary>
/// Uncaught node exception or other superstep fault that failed the run.
/// </summary>
public sealed class GraphRunFailedException : GraphException
{
    /// <summary>
    /// Initializes a failed-run exception.
    /// </summary>
    /// <param name="message">Human-readable description.</param>
    /// <param name="innerException">Node or apply failure.</param>
    public GraphRunFailedException(string message, Exception? innerException = null)
        : base("graph.run_failed", message, innerException)
    {
    }
}
