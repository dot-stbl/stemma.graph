// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Runtime.Exceptions;

/// <summary>
///     Uncaught node exception or other superstep fault that failed the run.
/// </summary>
/// <remarks>
///     Initializes a failed-run exception.
/// </remarks>
/// <param name="message">Human-readable description.</param>
/// <param name="innerException">Node or apply failure.</param>
public sealed class GraphRunFailedException(string message, Exception? innerException = null)
    : GraphException("graph.run_failed", message, innerException)
{
}
