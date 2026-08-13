// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Runtime.Exceptions;

/// <summary>
/// Resume was requested for a thread that is not interrupted.
/// </summary>
public sealed class GraphInvalidResumeException : GraphException
{
    /// <summary>
    /// Initializes an invalid-resume failure.
    /// </summary>
    /// <param name="message">Human-readable description.</param>
    public GraphInvalidResumeException(string message)
        : base("graph.invalid_resume", message)
    {
    }
}
