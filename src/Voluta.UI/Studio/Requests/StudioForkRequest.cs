namespace Voluta.UI.Studio;

/// <summary>
///     POST body for <c>POST /api/v1/threads/{id}/fork</c>.
/// </summary>
public sealed class StudioForkRequest
{
    /// <summary>
    ///     History step on the source thread to copy.
    /// </summary>
    public long Step { get; init; }

    /// <summary>
    ///     Destination thread id (must not already have a conflicting history root).
    /// </summary>
    public required string NewThreadId { get; init; }
}
