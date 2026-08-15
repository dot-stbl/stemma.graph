namespace Voluta.UI;

/// <summary>
///     Host options for the versioned Studio HTTP/SSE API (<c>MapStudioApi</c>).
/// </summary>
public sealed class StudioApiOptions
{
    /// <summary>
    ///     Config section name.
    /// </summary>
    public const string SectionName = "StudioApi";

    /// <summary>
    ///     URL path prefix (default <c>/api/v1</c>). Must include the version segment.
    /// </summary>
    public string PathPrefix { get; set; } = "/api/v1";

    /// <summary>
    ///     Optional shared API key. When null or empty, authentication is disabled (default).
    ///     When set, requests must send the key via <c>X-Api-Key</c> or
    ///     <c>Authorization: Bearer {key}</c>.
    /// </summary>
    public string? ApiKey { get; set; }
}
