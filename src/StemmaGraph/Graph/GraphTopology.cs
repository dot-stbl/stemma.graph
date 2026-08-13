// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Graph.Options;
using StemmaGraph.Abstractions.Channels;

namespace StemmaGraph.Graph;

/// <summary>
///     Immutable topology produced by <see cref="Builder.StateGraph.Compile" />.
/// </summary>
/// <remarks>
///     Initializes topology maps.
/// </remarks>
internal sealed class GraphTopology(
    IReadOnlyDictionary<string, NodeHandler> nodes,
    IReadOnlyDictionary<string, ChannelKind> channels,
    IReadOnlyDictionary<string, IReadOnlyList<string>> staticEdges,
    IReadOnlyDictionary<string, Func<GraphContext, IReadOnlyList<string>>> conditionalEdges,
    int recursionLimit)
{
    /// <summary>
    ///     Node name → handler.
    /// </summary>
    public IReadOnlyDictionary<string, NodeHandler> Nodes { get; } = nodes;

    /// <summary>
    ///     Channel name → kind.
    /// </summary>
    public IReadOnlyDictionary<string, ChannelKind> Channels { get; } = channels;

    /// <summary>
    ///     Source node → static target names (may include END).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> StaticEdges { get; } = staticEdges;

    /// <summary>
    ///     Source node → routing function selecting next node names (or END).
    /// </summary>
    public IReadOnlyDictionary<string, Func<GraphContext, IReadOnlyList<string>>> ConditionalEdges { get; } =
        conditionalEdges;

    /// <summary>
    ///     Maximum supersteps before out-of-steps failure.
    /// </summary>
    public int RecursionLimit { get; } = recursionLimit;
}
