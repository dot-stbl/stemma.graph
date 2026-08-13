using StemmaGraph.Abstractions.Channels;

namespace StemmaGraph.Abstractions.Topology;

/// <summary>
///     Read-only topology export for UI / tooling (no handlers).
/// </summary>
public sealed class GraphDescription
{
    /// <summary>
    ///     Registered node names (sorted).
    /// </summary>
    public IReadOnlyList<string> Nodes { get; init; } = [];

    /// <summary>
    ///     Channel name → merge kind.
    /// </summary>
    public IReadOnlyDictionary<string, ChannelKind> Channels { get; init; }
        = new Dictionary<string, ChannelKind>(StringComparer.Ordinal);

    /// <summary>
    ///     Static edges (including START/END endpoints).
    /// </summary>
    public IReadOnlyList<GraphEdgeDescription> StaticEdges { get; init; } = [];

    /// <summary>
    ///     Nodes that have conditional routers (target labels not statically known).
    /// </summary>
    public IReadOnlyList<string> ConditionalSources { get; init; } = [];

    /// <summary>
    ///     Configured recursion / superstep limit.
    /// </summary>
    public int RecursionLimit { get; init; }
}

/// <summary>
///     One static edge in a <see cref="GraphDescription" />.
/// </summary>
public sealed class GraphEdgeDescription
{
    /// <summary>
    ///     Source node or START.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    ///     Target node or END.
    /// </summary>
    public required string Target { get; init; }
}
