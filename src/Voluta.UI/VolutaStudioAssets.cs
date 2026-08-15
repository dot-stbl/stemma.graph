namespace Voluta.UI;

/// <summary>
///     Embedded SPA assets under <c>wwwroot/studio/</c> (Vite outDir). Optional — host stays up when missing.
/// </summary>
internal static class VolutaStudioAssets
{
    private const string ResourceSuffixPrefix = "wwwroot.studio.";

    private static readonly Lazy<bool> Available = new(static () => FindResourceName("index.html") is not null);

    /// <summary>
    ///     Whether <c>wwwroot/studio/index.html</c> is embedded (SPA was built into the package).
    /// </summary>
    public static bool IsAvailable => Available.Value;

    /// <summary>
    ///     HTML shell with <c>&lt;base href&gt;</c> so relative Vite assets resolve under <c>{prefix}/studio/</c>.
    /// </summary>
    /// <param name="pathPrefix">Normalized UI prefix, e.g. <c>/voluta</c>.</param>
    public static string? RenderIndexHtml(string pathPrefix)
    {
        if (TryReadUtf8("index.html") is not { } html)
        {
            return null;
        }

        var studioBase = pathPrefix.EndsWith('/')
            ? pathPrefix + "studio/"
            : pathPrefix + "/studio/";
        return InjectBaseHref(html, studioBase);
    }

    /// <summary>
    ///     Reads a studio-relative asset (e.g. <c>assets/index-abc.js</c>) as bytes.
    /// </summary>
    public static bool TryReadBytes(string relativePath, out byte[] bytes, out string contentType)
    {
        bytes = [];
        contentType = "application/octet-stream";
        if (NormalizeRelativePath(relativePath) is not { } normalized)
        {
            return false;
        }

        if (FindResourceName(normalized) is not { } resourceName)
        {
            return false;
        }

        var assembly = typeof(VolutaStudioAssets).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return false;
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        bytes = memory.ToArray();
        contentType = ContentTypeFor(normalized);
        return true;
    }

    /// <summary>
    ///     Placeholder when SPA dist is not embedded — do not crash the host.
    /// </summary>
    public static string RenderUnavailableHtml(string pathPrefix)
    {
        var studioUrl = pathPrefix.EndsWith('/') ? pathPrefix + "studio" : pathPrefix + "/studio";
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>Voluta Studio — not built</title>
              <style>
                body { font-family: system-ui, sans-serif; max-width: 40rem; margin: 3rem auto; padding: 0 1rem; line-height: 1.5; color: #1a1a1a; }
                code { background: #f4f4f5; padding: 0.1em 0.35em; border-radius: 4px; }
                pre { background: #f4f4f5; padding: 0.75rem 1rem; border-radius: 8px; overflow-x: auto; }
              </style>
            </head>
            <body>
              <h1>Studio SPA not built</h1>
              <p>
                Host is running, but embedded assets under <code>wwwroot/studio/</code> are missing.
                Build the SPA, then rebuild the UI package / host.
              </p>
              <pre>cd src/Voluta.UI/spa
            bun install
            bun run build
            # outDir → ../wwwroot/studio
            # then: dotnet build src/Voluta.UI</pre>
              <p>Expected URL after build: <code>{{studioUrl}}</code></p>
              <p>Legacy shell remains at the UI path prefix.</p>
            </body>
            </html>
            """;
    }

    private static string? TryReadUtf8(string relativePath)
    {
        return TryReadBytes(relativePath, out var bytes, out _)
            ? System.Text.Encoding.UTF8.GetString(bytes)
            : null;
    }

    private static string? FindResourceName(string relativePath)
    {
        var suffix = ResourceSuffixPrefix + relativePath.Replace('/', '.');
        var assembly = typeof(VolutaStudioAssets).Assembly;
        return assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeRelativePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var normalized = relativePath.Replace('\\', '/').Trim('/');
        return normalized.Length == 0
            || normalized.Contains("..", StringComparison.Ordinal)
            || normalized.StartsWith('/')
            || Path.IsPathRooted(normalized)
            ? null
            : normalized;
    }

    private static string InjectBaseHref(string html, string baseHref)
    {
        const string headOpen = "<head>";
        var injection = $"<head>\n    <base href=\"{baseHref}\" />";
        if (html.Contains(headOpen, StringComparison.OrdinalIgnoreCase))
        {
            var index = html.IndexOf(headOpen, StringComparison.OrdinalIgnoreCase);
            return html.Remove(index, headOpen.Length).Insert(index, injection);
        }

        return $"<!doctype html><base href=\"{baseHref}\" />" + html;
    }

    private static string ContentTypeFor(string relativePath)
    {
        return Path.GetExtension(relativePath).ToLowerInvariant() switch
        {
            ".html" => "text/html; charset=utf-8",
            ".js" or ".mjs" => "text/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".map" => "application/json; charset=utf-8",
            ".txt" => "text/plain; charset=utf-8",
            _ => "application/octet-stream",
        };
    }
}
