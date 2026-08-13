namespace StemmaGraph.UI;

/// <summary>
///     Host options for <c>MapStemmaUI</c>.
/// </summary>
public sealed class StemmaUiOptions
{
    /// <summary>
    ///     Config section name.
    /// </summary>
    public const string SectionName = "StemmaUI";

    /// <summary>
    ///     URL path prefix (default <c>/stemma</c>).
    /// </summary>
    public string PathPrefix { get; init; } = "/stemma";
}
