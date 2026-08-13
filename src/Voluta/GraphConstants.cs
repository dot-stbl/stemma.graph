namespace Voluta;

/// <summary>
///     Sentinel node names for graph entry and terminal edges.
/// </summary>
public static class GraphConstants
{
    /// <summary>
    ///     Virtual entry node; edges from START seed the first ready set.
    /// </summary>
    public const string Start = "__start__";

    /// <summary>
    ///     Virtual terminal node; edges to END do not schedule further tasks.
    /// </summary>
    public const string End = "__end__";
}
