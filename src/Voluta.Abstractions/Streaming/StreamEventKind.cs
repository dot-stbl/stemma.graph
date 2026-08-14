namespace Voluta.Abstractions.Streaming;

/// <summary>
///     Discriminator for lifecycle and observation stream items.
/// </summary>
public enum StreamEventKind
{
    /// <summary>
    ///     Unspecified or mode-default item.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Run or stream started.
    /// </summary>
    Start = 1,

    /// <summary>
    ///     Values-mode state snapshot after a commit.
    /// </summary>
    Values = 2,

    /// <summary>
    ///     Updates-mode channel write delta.
    /// </summary>
    Updates = 3,

    /// <summary>
    ///     Run interrupted for HITL.
    /// </summary>
    Interrupt = 4,

    /// <summary>
    ///     Run completed successfully.
    /// </summary>
    End = 5,

    /// <summary>
    ///     Run failed.
    /// </summary>
    Failed = 6,

    /// <summary>
    ///     Run cancelled.
    /// </summary>
    Cancelled = 7,

    /// <summary>
    ///     Custom progress or structured payload written by a node via <see cref="IStreamWriter" />.
    /// </summary>
    Custom = 8,

    /// <summary>
    ///     LLM token / message fragment written by a node via <see cref="IStreamWriter" />.
    /// </summary>
    Messages = 9
}
