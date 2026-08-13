namespace StemmaGraph.Abstractions.Channels;

/// <summary>
///     Built-in channel merge kinds for multi-writer supersteps.
/// </summary>
public enum ChannelKind
{
    /// <summary>
    ///     At most one write per superstep; concurrent multi-write fails the run.
    /// </summary>
    LastValue = 0,

    /// <summary>
    ///     Multiple writes per superstep are combined with the registered append reducer.
    /// </summary>
    Append = 1
}
