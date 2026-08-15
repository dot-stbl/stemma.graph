using NetArchTest.Rules;
using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Architecture.Unit;

/// <summary>
/// Package-boundary rules from
/// <c>openspec/specs/quality-engineering/spec.md</c>: Abstractions stays
/// dependency-free; core runtime never references provider/UI/Agents packages.
/// </summary>
public sealed class PackageIsolationShould
{
    private static readonly string[] ProviderAndUiProjects =
    [
        "Voluta.Checkpoints.EntityFrameworkCore",
        "Voluta.Checkpoints.S3",
        "Voluta.Checkpoints.File",
        "Voluta.Checkpoints.Redis",
        "Voluta.UI",
        "Voluta.Agents.AI",
        "Voluta.Generators",
        "Voluta.Testing",
    ];

    private static readonly string[] ForbiddenPackageIdFragments =
    [
        "EntityFrameworkCore",
        "AspNetCore",
        "AWSSDK",
        "StackExchange.Redis",
        "Microsoft.Agents",
        "Microsoft.Extensions.AI",
        "Voluta.Agents",
        "Voluta.Checkpoints",
        "Voluta.UI",
    ];

    [Fact(DisplayName = "Given Voluta.Abstractions, when ProjectReferences are read, then the list is empty")]
    public void AbstractionsHasNoProjectReferences()
    {
        var path = CsprojGraph.SrcProject("Voluta.Abstractions.csproj");

        CsprojGraph.ProjectReferenceNames(path).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Given Voluta.Abstractions, when PackageReferences are read, then the list is empty")]
    public void AbstractionsHasNoPackageReferences()
    {
        var path = CsprojGraph.SrcProject("Voluta.Abstractions.csproj");

        CsprojGraph.PackageReferenceIds(path).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Given Voluta.Abstractions, when ProjectReferences are scanned, then no forbidden packages appear")]
    public void AbstractionsHasNoForbiddenProjectReferences()
    {
        var path = CsprojGraph.SrcProject("Voluta.Abstractions.csproj");
        var forbidden = new[]
        {
            "Voluta",
            "Voluta.DependencyInjection",
            "Voluta.Checkpoints.EntityFrameworkCore",
            "Voluta.Checkpoints.S3",
            "Voluta.Checkpoints.File",
            "Voluta.Checkpoints.Redis",
            "Voluta.UI",
            "Voluta.Agents.AI",
            "Voluta.Generators",
            "Voluta.Testing",
        };

        var references = CsprojGraph.ProjectReferenceNames(path);
        references.ShouldNotContain(name => forbidden.Contains(name, StringComparer.Ordinal));
    }

    [Fact(DisplayName = "Given Voluta core, when ProjectReferences are read, then only Abstractions is referenced")]
    public void CoreReferencesOnlyAbstractions()
    {
        var path = CsprojGraph.SrcProject("Voluta.csproj");

        CsprojGraph.ProjectReferenceNames(path).ShouldBe(["Voluta.Abstractions"]);
    }

    [Fact(DisplayName = "Given Voluta core, when ProjectReferences are scanned, then no EF/S3/File/UI/Agents packages appear")]
    public void CoreHasNoProviderOrUiProjectReferences()
    {
        var path = CsprojGraph.SrcProject("Voluta.csproj");
        var references = CsprojGraph.ProjectReferenceNames(path);

        references.ShouldNotContain(name => ProviderAndUiProjects.Contains(name, StringComparer.Ordinal));
    }

    [Fact(DisplayName = "Given Voluta.DependencyInjection, when ProjectReferences are read, then only Voluta is referenced")]
    public void DependencyInjectionReferencesOnlyVoluta()
    {
        var path = CsprojGraph.SrcProject("Voluta.DependencyInjection.csproj");

        CsprojGraph.ProjectReferenceNames(path).ShouldBe(["Voluta"]);
    }

    [Fact(DisplayName = "Given Voluta.DependencyInjection, when ProjectReferences are scanned, then no provider/UI packages appear")]
    public void DependencyInjectionHasNoProviderOrUiProjectReferences()
    {
        var path = CsprojGraph.SrcProject("Voluta.DependencyInjection.csproj");
        var references = CsprojGraph.ProjectReferenceNames(path);

        references.ShouldNotContain(name => ProviderAndUiProjects.Contains(name, StringComparer.Ordinal));
    }

    [Fact(DisplayName = "Given Voluta.Agents.AI, when ProjectReferences are read, then Voluta is referenced and UI/EF are not")]
    public void AgentsAiMayReferenceVolutaButNotUiOrEf()
    {
        var path = CsprojGraph.SrcProject("Voluta.Agents.AI.csproj");
        var references = CsprojGraph.ProjectReferenceNames(path);

        references.ShouldContain("Voluta");
        references.ShouldNotContain("Voluta.UI");
        references.ShouldNotContain("Voluta.Checkpoints.EntityFrameworkCore");
        references.ShouldNotContain("Voluta.Checkpoints.S3");
        references.ShouldNotContain("Voluta.Checkpoints.File");
        references.ShouldNotContain("Voluta.Checkpoints.Redis");
    }

    [Fact(DisplayName = "Given Voluta.Checkpoints.Redis, when ProjectReferences are read, then core packages only")]
    public void RedisMayReferenceCoreButNotUiOrAgents()
    {
        var path = CsprojGraph.SrcProject("Voluta.Checkpoints.Redis.csproj");
        var references = CsprojGraph.ProjectReferenceNames(path);

        references.ShouldContain("Voluta.Abstractions");
        references.ShouldContain("Voluta.DependencyInjection");
        references.ShouldNotContain("Voluta.UI");
        references.ShouldNotContain("Voluta.Agents.AI");
    }

    [Fact(DisplayName = "Given Voluta.Abstractions assembly, when types are scanned, then they do not depend on EF/ASP.NET/AI packages")]
    public void AbstractionsAssemblyHasNoForbiddenTypeDependencies()
    {
        var result = Types
            .InAssembly(typeof(ICheckpointer).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Amazon.S3",
                "StackExchange.Redis",
                "Microsoft.Agents.AI",
                "Microsoft.Extensions.AI",
                "Voluta.Checkpoints.EntityFrameworkCore",
                "Voluta.Checkpoints.S3",
                "Voluta.Checkpoints.File",
                "Voluta.Checkpoints.Redis",
                "Voluta.UI",
                "Voluta.Agents.AI")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            result.FailingTypeNames is { } names
                ? string.Join(", ", names)
                : "unknown dependency violation");
    }

    [Fact(DisplayName = "Given Voluta core assembly, when types are scanned, then they do not depend on EF/S3/UI/Agents packages")]
    public void CoreAssemblyHasNoForbiddenTypeDependencies()
    {
        var result = Types
            .InAssembly(typeof(StateGraph).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Amazon.S3",
                "StackExchange.Redis",
                "Microsoft.Agents.AI",
                "Microsoft.Extensions.AI",
                "Voluta.Checkpoints.EntityFrameworkCore",
                "Voluta.Checkpoints.S3",
                "Voluta.Checkpoints.File",
                "Voluta.Checkpoints.Redis",
                "Voluta.UI",
                "Voluta.Agents.AI")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            result.FailingTypeNames is { } names
                ? string.Join(", ", names)
                : "unknown dependency violation");
    }

    [Fact(DisplayName = "Given core package files, when PackageReferences are scanned, then no provider/UI package ids appear")]
    public void CoreAndAbstractionsPackageIdsStayClean()
    {
        foreach (var projectFile in new[] { "Voluta.Abstractions.csproj", "Voluta.csproj", "Voluta.DependencyInjection.csproj" })
        {
            var path = CsprojGraph.SrcProject(projectFile);
            var packageIds = CsprojGraph.PackageReferenceIds(path);
            var offenders = packageIds
                .Where(id => ForbiddenPackageIdFragments.Any(
                    fragment => id.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            offenders.ShouldBeEmpty($"{projectFile} must not PackageReference provider/UI/AI packages: {string.Join(", ", offenders)}");
        }
    }
}
