using System.Xml.Linq;
using Shouldly;

namespace Voluta.Architecture.Unit;

/// <summary>
/// Reads <c>ProjectReference</c> / <c>PackageReference</c> items from a
/// package <c>.csproj</c>. Source of truth for isolation rules is the
/// project file graph, not runtime assembly load order.
/// </summary>
internal static class CsprojGraph
{
    private static readonly XName ItemGroup = XName.Get("ItemGroup");
    private static readonly XName ProjectReference = XName.Get("ProjectReference");
    private static readonly XName PackageReference = XName.Get("PackageReference");

    public static string RepoRoot { get; } = LocateRepoRoot();

    public static string SrcProject(string projectFileName)
    {
        return Path.Combine(RepoRoot, "src", Path.GetFileNameWithoutExtension(projectFileName), projectFileName);
    }

    public static IReadOnlyList<string> ProjectReferenceNames(string csprojPath)
    {
        return ReadIncludes(csprojPath, ProjectReference)
            .Select(static include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<string> PackageReferenceIds(string csprojPath)
    {
        return ReadIncludes(csprojPath, PackageReference)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> ReadIncludes(string csprojPath, XName elementName)
    {
        File.Exists(csprojPath).ShouldBeTrue($"expected csproj at {csprojPath}");

        var document = XDocument.Load(csprojPath);
        var root = document.Root;
        root.ShouldNotBeNull();

        return root
            .Elements(ItemGroup)
            .Elements(elementName)
            .Select(static element => (string?)element.Attribute("Include") ?? string.Empty)
            .Where(static include => include.Length > 0);
    }

    private static string LocateRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "voluta.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"could not locate voluta.slnx walking up from {AppContext.BaseDirectory}");
    }
}
