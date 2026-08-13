// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Abstractions.Channels;

namespace StemmaGraph.Abstractions.State;

/// <summary>
///     One channel registration entry produced by hand or by source generation.
/// </summary>
/// <param name="name">Channel name in the graph state map.</param>
/// <param name="kind">Merge kind for multi-writer supersteps.</param>
public sealed class GraphChannelDeclaration(string name, ChannelKind kind)
{
    /// <summary>
    ///     Channel name in the graph state map.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    ///     Merge kind for multi-writer supersteps.
    /// </summary>
    public ChannelKind Kind { get; } = kind;
}
