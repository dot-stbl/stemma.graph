// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Runtime.Exceptions;

/// <summary>
/// LastValue multi-writer violation within a single superstep.
/// </summary>
public sealed class GraphConcurrentUpdateException : GraphException
{
    /// <summary>
    /// Initializes a concurrent-update failure.
    /// </summary>
    /// <param name="message">Human-readable description.</param>
    public GraphConcurrentUpdateException(string message)
        : base("channel.concurrent_update", message)
    {
    }
}
