using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using StemmaGraph.Abstractions.Runtime;

namespace StemmaGraph.UI;

/// <summary>
///     ASP.NET Core endpoint mapping for the Stemma ops UI.
/// </summary>
public static class StemmaUiEndpointRouteBuilderExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    ///     Registers <see cref="StemmaUiSession" /> as singleton.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="session">Bound session.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddStemmaUI(this IServiceCollection services, StemmaUiSession session)
    {
        services.AddSingleton(session);
        return services;
    }

    /// <summary>
    ///     Maps static UI + JSON API under the given path prefix (default <c>/stemma</c>).
    /// </summary>
    /// <param name="endpoints">Endpoint route builder (typically <see cref="WebApplication" />).</param>
    /// <param name="pathPrefix">URL prefix.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapStemmaUI(
        this IEndpointRouteBuilder endpoints,
        string pathPrefix = "/stemma")
    {
        var prefix = pathPrefix.TrimEnd('/');
        if (string.IsNullOrEmpty(prefix))
        {
            prefix = "/stemma";
        }

        endpoints.MapGet(prefix, () => Results.Redirect($"{prefix}/"));
        endpoints.MapGet($"{prefix}/", () => Results.Content(ReadWww("index.html"), "text/html; charset=utf-8"));
        endpoints.MapGet(
            $"{prefix}/index.html",
            () => Results.Content(ReadWww("index.html"), "text/html; charset=utf-8"));
        endpoints.MapGet(
            $"{prefix}/styles.css",
            () => Results.Content(ReadWww("styles.css"), "text/css; charset=utf-8"));

        endpoints.MapGet(
            $"{prefix}/api/topology",
            (StemmaUiSession session) => Results.Json(session.Topology, JsonOptions));

        endpoints.MapGet(
            $"{prefix}/api/hitl",
            async (StemmaUiSession session, CancellationToken cancellationToken) =>
                Results.Json(await session.ListInterruptedAsync(cancellationToken), JsonOptions));

        endpoints.MapGet(
            $"{prefix}/api/threads/{{threadId}}",
            async (string threadId, StemmaUiSession session, CancellationToken cancellationToken) =>
            {
                var snapshot = await session.GetCheckpointAsync(threadId, cancellationToken);
                return snapshot is null
                    ? Results.NotFound()
                    : Results.Json(snapshot, JsonOptions);
            });

        endpoints.MapPost(
            $"{prefix}/api/threads/{{threadId}}/resume",
            async (
                string threadId,
                ResumeRequest? body,
                StemmaUiSession session,
                CancellationToken cancellationToken) =>
            {
                var command = new Command
                {
                    Kind = body?.Kind ?? "approve",
                    Payload = body?.Payload,
                };
                var terminal = await session.ResumeAsync(threadId, command, cancellationToken);
                return Results.Json(
                    new
                    {
                        kind = terminal.Kind.ToString(),
                        step = terminal.Step,
                        payload = terminal.Payload?.ToString(),
                    },
                    JsonOptions);
            });

        return endpoints;
    }

    private static string ReadWww(string fileName)
    {
        var assembly = typeof(StemmaUiEndpointRouteBuilderExtensions).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            throw new InvalidOperationException($"Embedded UI resource not found: {fileName}");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Cannot open resource {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

/// <summary>
///     Resume POST body.
/// </summary>
public sealed class ResumeRequest
{
    /// <summary>
    ///     Command kind (default approve).
    /// </summary>
    public string? Kind { get; init; }

    /// <summary>
    ///     Optional payload.
    /// </summary>
    public string? Payload { get; init; }
}
