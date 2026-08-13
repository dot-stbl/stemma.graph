// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Runtime.Exceptions;

/// <summary>
/// Topology validation failure raised during <see cref="Graph.StateGraph.Compile"/>.
/// </summary>
public sealed class GraphCompileException : GraphException
{
    /// <summary>
    /// Initializes a compile-time graph exception.
    /// </summary>
    /// <param name="code">Stable error code.</param>
    /// <param name="message">Validation message.</param>
    public GraphCompileException(string code, string message)
        : base(code, message)
    {
    }
}
