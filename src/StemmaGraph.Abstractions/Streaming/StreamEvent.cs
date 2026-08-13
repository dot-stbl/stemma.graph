using StemmaGraph.Abstractions.Channels;

namespace StemmaGraph.Abstractions.Streaming;

/// <summary>
///     Single item in a multi-mode graph stream (values, updates, or lifecycle events).
/// </summary>
public sealed class StreamEvent
{
    /// <summary>
    ///     Stream mode that produced this item.
    /// </summary>
    public StreamMode Mode { get; init; }

    /// <summary>
    ///     Lifecycle / observation kind for events-mode and terminal signals.
    /// </summary>
    public StreamEventKind Kind { get; init; }

    /// <summary>
    ///     Superstep index associated with this item, when applicable.
    /// </summary>
    public long Step { get; init; }

    /// <summary>
    ///     Node names that produced updates, when applicable.
    /// </summary>
    public IReadOnlyList<string> NodeNames { get; init; } = [];

    /// <summary>
    ///     Channel writes for updates-mode items.
    /// </summary>
    public IReadOnlyList<ChannelWrite> Writes { get; init; } = [];

    /// <summary>
    ///     Channel state snapshot for values-mode items (name → value).
    /// </summary>
    public IReadOnlyDictionary<string, object?>? State { get; init; }

    /// <summary>
    ///     Optional interrupt or fault payload.
    /// </summary>
    public object? Payload { get; init; }
}
