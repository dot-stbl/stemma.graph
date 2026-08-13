// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Channels;

namespace StemmaGraph.Results;

/// <summary>
/// Successful node completion carrying partial channel writes for apply_writes.
/// </summary>
public sealed class ContinueNodeResult : NodeResult
{
    /// <summary>
    /// Initializes a continue result.
    /// </summary>
    /// <param name="writes">Partial channel updates; empty means no channel changes.</param>
    public ContinueNodeResult(IReadOnlyList<ChannelWrite> writes)
    {
        Writes = writes;
    }

    /// <summary>
    /// Partial channel writes produced by the node.
    /// </summary>
    public IReadOnlyList<ChannelWrite> Writes { get; }
}
