namespace Voluta.Samples.Shared;

/// <summary>
///     File access restricted to a single root directory (no path escape).
/// </summary>
public sealed class SandboxFileSystem
{
    public SandboxFileSystem(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Sandbox root is required.", nameof(rootDirectory));
        }

        Root = Path.GetFullPath(rootDirectory);
        if (!Directory.Exists(Root))
        {
            throw new DirectoryNotFoundException($"Sandbox root not found: {Root}");
        }
    }

    public string Root { get; }

    public string Resolve(string relativeOrUnderRoot)
    {
        var combined = Path.IsPathRooted(relativeOrUnderRoot)
            ? Path.GetFullPath(relativeOrUnderRoot)
            : Path.GetFullPath(Path.Combine(Root, relativeOrUnderRoot));

        var rootWithSep = Root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                          + Path.DirectorySeparatorChar;

        return !combined.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase)
               && !string.Equals(combined, Root, StringComparison.OrdinalIgnoreCase)
            ? throw new UnauthorizedAccessException(
                $"Path escapes sandbox: '{relativeOrUnderRoot}' → '{combined}' (root '{Root}').")
            : combined;
    }

    public string ReadAllText(string relativePath)
    {
        var path = Resolve(relativePath);
        return File.Exists(path)
            ? File.ReadAllText(path)
            : throw new FileNotFoundException($"Not found in sandbox: {relativePath}", path);
    }

    public IReadOnlyList<string> ListFiles(string searchPattern = "*")
    {
        return [.. Directory
            .EnumerateFiles(Root, searchPattern, SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(Root, path).Replace('\\', '/'))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)];
    }

    public IReadOnlyList<string> Search(string query, string searchPattern = "*.md", int maxHits = 40)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var hits = new List<string>();
        foreach (var relative in ListFiles(searchPattern))
        {
            if (ReadAllText(relative).Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                hits.Add(relative);
                if (hits.Count >= maxHits)
                {
                    break;
                }
            }
        }

        return hits;
    }
}
