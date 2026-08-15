namespace Voluta.UI;

/// <summary>
///     Host options for <c>MapVolutaUI</c>.
/// </summary>
public sealed class VolutaUiOptions
{
    /// <summary>
    ///     Config section name.
    /// </summary>
    public const string SectionName = "VolutaUI";

    /// <summary>
    ///     URL path prefix (default <c>/voluta</c>).
    /// </summary>
    public string PathPrefix { get; set; } = "/voluta";
}
