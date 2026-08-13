// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Abstractions.Channels;

namespace StemmaGraph.Abstractions.Results;

/// <summary>
///     Successful node completion carrying partial channel writes for apply_writes.
/// </summary>
/// <remarks>
///     Initializes a continue result.
/// </remarks>
/// <param name="writes">Partial channel updates; empty means no channel changes.</param>
public sealed class ContinueNodeResult(IReadOnlyList<ChannelWrite> writes) : NodeResult
{
    /// <summary>
    ///     Partial channel writes produced by the node.
    /// </summary>
    public IReadOnlyList<ChannelWrite> Writes { get; } = writes;
}
