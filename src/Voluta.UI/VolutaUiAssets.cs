namespace Voluta.UI;

/// <summary>
///     Embedded wwwroot assets for the ops console shell.
/// </summary>
internal static class VolutaUiAssets
{
    private static readonly string IndexHtmlTemplate = VolutaUiAssetReader.Read("wwwroot.index.html");

    public static string StylesCss { get; } = VolutaUiAssetReader.Read("wwwroot.styles.css");

    public static string AppJs { get; } = VolutaUiAssetReader.Read("wwwroot.app.js");

    /// <summary>
    ///     HTML shell with a <c>&lt;base href&gt;</c> so relative asset URLs resolve under the UI prefix
    ///     even when the browser path is <c>/voluta</c> (no trailing slash).
    /// </summary>
    /// <param name="pathPrefix">Normalized prefix, e.g. <c>/voluta</c>.</param>
    public static string RenderIndexHtml(string pathPrefix)
    {
        var baseHref = pathPrefix.EndsWith('/') ? pathPrefix : pathPrefix + "/";
        const string marker = "<title>Voluta · ops</title>";
        var injection = $"<title>Voluta · ops</title>\n  <base href=\"{baseHref}\" />";
        return IndexHtmlTemplate.Contains(marker, StringComparison.Ordinal)
            ? IndexHtmlTemplate.Replace(marker, injection, StringComparison.Ordinal)
            : $"<!doctype html><base href=\"{baseHref}\" />" + IndexHtmlTemplate;
    }
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
