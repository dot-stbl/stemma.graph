namespace Voluta.Abstractions.Channels;

/// <summary>
///     A partial channel update emitted by a node (or resume command).
///     Omitted channels are unchanged; an explicit write with a null value is a clear.
/// </summary>
public sealed class ChannelWrite(string channelName, object? value)
{
    /// <summary>
    ///     Target channel name within the graph state map.
    /// </summary>
    public string ChannelName { get; } = channelName;

    /// <summary>
    ///     Value to apply via the channel reducer. Null is an explicit clear, not “unchanged”.
    /// </summary>
    public object? Value { get; } = value;
}
