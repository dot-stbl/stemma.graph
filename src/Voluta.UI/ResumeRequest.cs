namespace Voluta.UI;

/// <summary>
///     Resume POST body for HITL actions.
/// </summary>
public sealed class ResumeRequest
{
    /// <summary>
    ///     Command kind (default approve).
    /// </summary>
    public string? Kind { get; init; }

    /// <summary>
    ///     Optional payload.
    /// </summary>
    public string? Payload { get; init; }
}
