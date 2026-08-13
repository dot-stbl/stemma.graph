// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Channels;

namespace StemmaGraph.Graph;

/// <summary>
/// Immutable topology produced by <see cref="StateGraph.Compile"/>.
/// </summary>
internal sealed class GraphTopology
{
    /// <summary>
    /// Initializes topology maps.
    /// </summary>
    public GraphTopology(
        IReadOnlyDictionary<string, NodeHandler> nodes,
        IReadOnlyDictionary<string, ChannelKind> channels,
        IReadOnlyDictionary<string, IReadOnlyList<string>> staticEdges,
        IReadOnlyDictionary<string, Func<GraphContext, IReadOnlyList<string>>> conditionalEdges,
        int recursionLimit)
    {
        Nodes = nodes;
        Channels = channels;
        StaticEdges = staticEdges;
        ConditionalEdges = conditionalEdges;
        RecursionLimit = recursionLimit;
    }

    /// <summary>
    /// Node name → handler.
    /// </summary>
    public IReadOnlyDictionary<string, NodeHandler> Nodes { get; }

    /// <summary>
    /// Channel name → kind.
    /// </summary>
    public IReadOnlyDictionary<string, ChannelKind> Channels { get; }

    /// <summary>
    /// Source node → static target names (may include END).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> StaticEdges { get; }

    /// <summary>
    /// Source node → routing function selecting next node names (or END).
    /// </summary>
    public IReadOnlyDictionary<string, Func<GraphContext, IReadOnlyList<string>>> ConditionalEdges { get; }

    /// <summary>
    /// Maximum supersteps before out-of-steps failure.
    /// </summary>
    public int RecursionLimit { get; }
}
