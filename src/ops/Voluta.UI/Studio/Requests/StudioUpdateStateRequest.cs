namespace Voluta.UI.Studio;

/// <summary>
///     POST body for <c>POST /api/v1/threads/{id}/update</c>.
/// </summary>
public sealed class StudioUpdateStateRequest
{
    /// <summary>
    ///     Channel writes to merge into the latest checkpoint (reducer-aware).
    /// </summary>
    public IReadOnlyList<StudioChannelWrite>? Writes { get; init; }
}
