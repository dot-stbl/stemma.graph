namespace Voluta.UI;

/// <summary>
///     Embedded wwwroot assets for the ops console shell.
/// </summary>
internal static class VolutaUiAssets
{
    public static string IndexHtml { get; } = VolutaUiAssetReader.Read("wwwroot.index.html");

    public static string StylesCss { get; } = VolutaUiAssetReader.Read("wwwroot.styles.css");

    public static string AppJs { get; } = VolutaUiAssetReader.Read("wwwroot.app.js");
}

/// <summary>
///     Manifest resource loader for UI static assets.
/// </summary>
file static class VolutaUiAssetReader
{
    public static string Read(string relativeName)
    {
        var assembly = typeof(VolutaUiAssets).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(relativeName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Embedded UI resource not found ending with: {relativeName}");

        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Embedded UI resource stream null: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
