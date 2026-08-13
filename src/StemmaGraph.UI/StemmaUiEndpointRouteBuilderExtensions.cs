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
        endpoints.MapGet($"{prefix}/", () => Results.Content(StemmaUiAssets.IndexHtml, "text/html; charset=utf-8"));
        endpoints.MapGet(
            $"{prefix}/index.html",
            () => Results.Content(StemmaUiAssets.IndexHtml, "text/html; charset=utf-8"));
        endpoints.MapGet(
            $"{prefix}/styles.css",
            () => Results.Content(StemmaUiAssets.StylesCss, "text/css; charset=utf-8"));

        endpoints.MapGet(
            $"{prefix}/api/topology",
            (StemmaUiSession session) => Results.Json(session.Topology, JsonSerializerOptions.Web));

        endpoints.MapGet(
            $"{prefix}/api/hitl",
            async (StemmaUiSession session, CancellationToken cancellationToken) =>
                Results.Json(await session.ListInterruptedAsync(cancellationToken), JsonSerializerOptions.Web));

        endpoints.MapGet(
            $"{prefix}/api/threads/{{threadId}}",
            async (string threadId, StemmaUiSession session, CancellationToken cancellationToken) =>
            {
                var snapshot = await session.GetCheckpointAsync(threadId, cancellationToken);
                return snapshot is null
                    ? Results.NotFound()
                    : Results.Json(snapshot, JsonSerializerOptions.Web);
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
                    JsonSerializerOptions.Web);
            });

        return endpoints;
    }
}
