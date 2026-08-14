using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Voluta.Abstractions.Streaming;

namespace Voluta.UI.Sse;

/// <summary>
///     Writes <see cref="StreamEvent" /> items as <c>text/event-stream</c> frames.
/// </summary>
internal static class StreamEventSseWriter
{
    public static async Task WriteAsync(
        HttpResponse response,
        IAsyncEnumerable<StreamEvent> stream,
        CancellationToken cancellationToken)
    {
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers.CacheControl = "no-cache";
        response.Headers.Connection = "keep-alive";
        response.Headers["X-Accel-Buffering"] = "no";

        await response.StartAsync(cancellationToken);

        await foreach (var item in stream.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payload = JsonSerializer.Serialize(VolutaUiJson.ToWire(item), JsonSerializerOptions.Web);
            await response.WriteAsync($"event: stream\ndata: {payload}\n\n", cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }

        await response.WriteAsync("event: done\ndata: {}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }

    public static async Task WriteErrorAsync(
        HttpResponse response,
        string message,
        CancellationToken cancellationToken)
    {
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers.CacheControl = "no-cache";
        await response.StartAsync(cancellationToken);
        var escaped = message.Replace("\n", " ", StringComparison.Ordinal);
        await response.WriteAsync($"event: error\ndata: {JsonSerializer.Serialize(new { message = escaped }, JsonSerializerOptions.Web)}\n\n", Encoding.UTF8, cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
