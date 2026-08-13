// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Samples.Shared;

/// <summary>
///     File access restricted to a single root directory (no path escape).
/// </summary>
public sealed class SandboxFileSystem
{
    private readonly string rootFull;

    public SandboxFileSystem(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Sandbox root is required.", nameof(rootDirectory));
        }

        rootFull = Path.GetFullPath(rootDirectory);
        if (!Directory.Exists(rootFull))
        {
            throw new DirectoryNotFoundException($"Sandbox root not found: {rootFull}");
        }
    }

    public string Root => rootFull;

    public string Resolve(string relativeOrUnderRoot)
    {
        var combined = Path.IsPathRooted(relativeOrUnderRoot)
            ? Path.GetFullPath(relativeOrUnderRoot)
            : Path.GetFullPath(Path.Combine(rootFull, relativeOrUnderRoot));

        var rootWithSep = rootFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                          + Path.DirectorySeparatorChar;

        return !combined.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase)
               && !string.Equals(combined, rootFull, StringComparison.OrdinalIgnoreCase)
            ? throw new UnauthorizedAccessException(
                $"Path escapes sandbox: '{relativeOrUnderRoot}' → '{combined}' (root '{rootFull}').")
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
        return Directory
            .EnumerateFiles(rootFull, searchPattern, SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(rootFull, path).Replace('\\', '/'))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
