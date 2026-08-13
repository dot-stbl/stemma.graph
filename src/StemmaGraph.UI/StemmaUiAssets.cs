namespace StemmaGraph.UI;

/// <summary>
///     Embedded wwwroot assets for the ops console.
/// </summary>
internal static class StemmaUiAssets
{
    public static string IndexHtml { get; } = Read("StemmaGraph.UI.wwwroot.index.html");

    public static string StylesCss { get; } = Read("StemmaGraph.UI.wwwroot.styles.css");

    private static string Read(string resourceName)
    {
        var assembly = typeof(StemmaUiAssets).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Embedded UI resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
