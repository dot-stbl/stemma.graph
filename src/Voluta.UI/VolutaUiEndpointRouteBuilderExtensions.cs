using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Voluta.Abstractions.Runtime;

namespace Voluta.UI;

/// <summary>
///     ASP.NET Core endpoint mapping for the Voluta ops UI.
/// </summary>
public static class VolutaUiEndpointRouteBuilderExtensions
{
    /// <summary>
    ///     Registers <see cref="VolutaUiSession" /> as singleton.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="session">Bound session.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddVolutaUI(this IServiceCollection services, VolutaUiSession session)
    {
        services.AddSingleton(session);
        return services;
    }

    /// <summary>
    ///     Maps static UI + JSON API under the given path prefix (default <c>/voluta</c>).
    /// </summary>
    /// <param name="endpoints">Endpoint route builder (typically <see cref="WebApplication" />).</param>
    /// <param name="pathPrefix">URL prefix.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapVolutaUI(
        this IEndpointRouteBuilder endpoints,
        string pathPrefix = "/voluta")
    {
        var prefix = pathPrefix.TrimEnd('/');
        if (string.IsNullOrEmpty(prefix))
        {
            prefix = "/voluta";
        }

        endpoints.MapGet(prefix, () => Results.Redirect($"{prefix}/"));
        endpoints.MapGet($"{prefix}/", () => Results.Content(VolutaUiAssets.IndexHtml, "text/html; charset=utf-8"));
        endpoints.MapGet(
            $"{prefix}/index.html",
            () => Results.Content(VolutaUiAssets.IndexHtml, "text/html; charset=utf-8"));
        endpoints.MapGet(
            $"{prefix}/styles.css",
            () => Results.Content(VolutaUiAssets.StylesCss, "text/css; charset=utf-8"));

        endpoints.MapGet(
            $"{prefix}/api/topology",
            (VolutaUiSession session) => Results.Json(session.Topology, JsonSerializerOptions.Web));

        endpoints.MapGet(
            $"{prefix}/api/hitl",
            async (VolutaUiSession session, CancellationToken cancellationToken) =>
                Results.Json(await session.ListInterruptedAsync(cancellationToken), JsonSerializerOptions.Web));

        endpoints.MapGet(
            $"{prefix}/api/threads/{{threadId}}",
            async (string threadId, VolutaUiSession session, CancellationToken cancellationToken) =>
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
                VolutaUiSession session,
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
