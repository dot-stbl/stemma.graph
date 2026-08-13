using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.UI.Sse;

namespace Voluta.UI;

/// <summary>
///     ASP.NET Core endpoint mapping for the Voluta ops UI (Swagger-style host API).
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
        _ = services.AddSingleton(session);
        return services;
    }

    /// <summary>
    ///     Maps UI shell + JSON API + SSE under the configured path prefix (default <c>/voluta</c>).
    /// </summary>
    /// <param name="endpoints">Endpoint route builder (typically <see cref="WebApplication" />).</param>
    /// <param name="configure">Optional options mutator (e.g. path prefix).</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapVolutaUI(
        this IEndpointRouteBuilder endpoints,
        Action<VolutaUiOptions>? configure = null)
    {
        var options = new VolutaUiOptions();
        configure?.Invoke(options);
        return MapVolutaUI(endpoints, options);
    }

    /// <summary>
    ///     Maps UI shell + JSON API + SSE under the given options.
    /// </summary>
    /// <param name="endpoints">Endpoint route builder.</param>
    /// <param name="options">UI host options.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapVolutaUI(
        this IEndpointRouteBuilder endpoints,
        VolutaUiOptions options)
    {
        var prefix = VolutaUiRouteHelpers.NormalizePrefix(options.PathPrefix);

        // Single shell route only — MapGet(prefix) and MapGet(prefix+"/") both match
        // /voluta and /voluta/ and throw AmbiguousMatchException.
        _ = endpoints.MapGet(
            prefix,
            () => Results.Content(VolutaUiAssets.IndexHtml, "text/html; charset=utf-8"));
        _ = endpoints.MapGet(
            $"{prefix}/index.html",
            () => Results.Content(VolutaUiAssets.IndexHtml, "text/html; charset=utf-8"));
        _ = endpoints.MapGet(
            $"{prefix}/styles.css",
            () => Results.Content(VolutaUiAssets.StylesCss, "text/css; charset=utf-8"));
        _ = endpoints.MapGet(
            $"{prefix}/app.js",
            () => Results.Content(VolutaUiAssets.AppJs, "text/javascript; charset=utf-8"));

        _ = endpoints.MapGet(
            $"{prefix}/api/topology",
            (VolutaUiSession session) =>
                Results.Json(VolutaUiJson.ToWire(session.Topology), JsonSerializerOptions.Web));

        _ = endpoints.MapGet(
            $"{prefix}/api/hitl",
            async (VolutaUiSession session, CancellationToken cancellationToken) =>
                Results.Json(await session.ListInterruptedAsync(cancellationToken), JsonSerializerOptions.Web));

        _ = endpoints.MapGet(
            $"{prefix}/api/threads/{{threadId}}",
            async (string threadId, VolutaUiSession session, CancellationToken cancellationToken) =>
            {
                var snapshot = await session.GetCheckpointAsync(threadId, cancellationToken);
                return snapshot is null
                    ? Results.NotFound()
                    : Results.Json(VolutaUiJson.ToWire(snapshot), JsonSerializerOptions.Web);
            });

        _ = endpoints.MapPost(
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
                return Results.Json(VolutaUiJson.ToWireTerminal(terminal), JsonSerializerOptions.Web);
            });

        _ = endpoints.MapGet(
            $"{prefix}/api/threads/{{threadId}}/stream",
            VolutaUiStreamEndpoint.HandleAsync);

        return endpoints;
    }

    /// <summary>
    ///     Legacy overload: maps UI under an explicit path prefix string.
    /// </summary>
    /// <param name="endpoints">Endpoint route builder.</param>
    /// <param name="pathPrefix">URL prefix.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapVolutaUI(
        this IEndpointRouteBuilder endpoints,
        string pathPrefix)
    {
        return MapVolutaUI(endpoints, new VolutaUiOptions { PathPrefix = pathPrefix });
    }
}

/// <summary>
///     Path prefix normalization for MapVolutaUI.
/// </summary>
file static class VolutaUiRouteHelpers
{
    public static string NormalizePrefix(string? pathPrefix)
    {
        var prefix = (pathPrefix ?? "/voluta").TrimEnd('/');
        return string.IsNullOrEmpty(prefix)
            ? "/voluta"
            : prefix.StartsWith('/') ? prefix : "/" + prefix;
    }
}

/// <summary>
///     SSE stream endpoint handler for live graph events.
/// </summary>
file static class VolutaUiStreamEndpoint
{
    public static async Task HandleAsync(
        string threadId,
        HttpContext httpContext,
        VolutaUiSession session,
        CancellationToken cancellationToken)
    {
        var mode = httpContext.Request.Query["mode"].FirstOrDefault() ?? "checkpoint";
        var kind = httpContext.Request.Query["kind"].FirstOrDefault() ?? "approve";
        var payload = httpContext.Request.Query["payload"].FirstOrDefault();

        IAsyncEnumerable<StreamEvent> stream;
        if (string.Equals(mode, "resume", StringComparison.OrdinalIgnoreCase))
        {
            stream = session.StreamResumeAsync(
                threadId,
                new Command { Kind = kind, Payload = payload },
                cancellationToken);
        }
        else if (string.Equals(mode, "invoke", StringComparison.OrdinalIgnoreCase))
        {
            var seed = httpContext.Request.Query["seed"].FirstOrDefault()
                       ?? "user: transfer $50";
            stream = session.StreamInvokeAsync(
                threadId,
                [new ChannelWrite("messages", seed)],
                cancellationToken);
        }
        else
        {
            var snapshot = await session.GetCheckpointAsync(threadId, cancellationToken);
            if (snapshot is null)
            {
                await StreamEventSseWriter.WriteErrorAsync(
                    httpContext.Response,
                    $"thread '{threadId}' not found",
                    cancellationToken);
                return;
            }

            stream = snapshot.Status == GraphRunStatus.Interrupted
                && string.Equals(
                    httpContext.Request.Query["auto"].FirstOrDefault(),
                    "1",
                    StringComparison.Ordinal)
                ? session.StreamResumeAsync(
                    threadId,
                    new Command { Kind = kind, Payload = payload ?? "ok" },
                    cancellationToken)
                : CheckpointAsStream.FromSnapshotAsync(snapshot);
        }

        await StreamEventSseWriter.WriteAsync(httpContext.Response, stream, cancellationToken);
    }
}

/// <summary>
///     Emits a single synthetic stream event from a checkpoint snapshot.
/// </summary>
file static class CheckpointAsStream
{
    public static async IAsyncEnumerable<StreamEvent> FromSnapshotAsync(
        Abstractions.Checkpoint.CheckpointSnapshot snapshot)
    {
        yield return new StreamEvent
        {
            Mode = StreamMode.Events,
            Kind = snapshot.Status switch
            {
                GraphRunStatus.Interrupted => StreamEventKind.Interrupt,
                GraphRunStatus.Done => StreamEventKind.End,
                GraphRunStatus.Failed => StreamEventKind.Failed,
                GraphRunStatus.Cancelled => StreamEventKind.Cancelled,
                GraphRunStatus.Running => throw new NotImplementedException(),
                _ => StreamEventKind.Values,
            },
            Step = snapshot.Step,
            NodeNames = snapshot.LastNode is { } last ? [last] : [],
            State = snapshot.ChannelValues,
            Payload = snapshot.InterruptPayload,
        };
        await Task.CompletedTask;
    }
}
