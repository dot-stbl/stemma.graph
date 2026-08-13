namespace Voluta.UI;

/// <summary>
///     Embedded wwwroot assets for the ops console.
/// </summary>
internal static class VolutaUiAssets
{
    public static string IndexHtml { get; } = Read("Voluta.UI.wwwroot.index.html");

    public static string StylesCss { get; } = Read("Voluta.UI.wwwroot.styles.css");

    private static string Read(string resourceName)
    {
        var assembly = typeof(VolutaUiAssets).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Embedded UI resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
