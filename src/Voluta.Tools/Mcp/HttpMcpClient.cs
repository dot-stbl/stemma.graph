using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Voluta.Tools.Tools;

namespace Voluta.Tools.Mcp;

/// <summary>
///     HTTP client for the demo MCP-shaped surface used by MockAdMcp / MarketingAgent
///     (<c>GET /mcp/tools</c>, <c>POST /mcp/tools/call</c>).
/// </summary>
public sealed class HttpMcpClient(HttpClient http) : IMcpClient, IDisposable
{
    private bool disposed;

    /// <summary>
    ///     Creates a client with a dedicated <see cref="HttpClient" /> for <paramref name="baseUrl" />.
    /// </summary>
    /// <param name="baseUrl">Service base URL (e.g. <c>http://localhost:5190</c>).</param>
    /// <param name="timeout">Optional request timeout (default 15s).</param>
    /// <returns>Disposable MCP client.</returns>
    public static HttpMcpClient Create(string baseUrl, TimeSpan? timeout = null)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = timeout ?? TimeSpan.FromSeconds(15),
        };
        return new HttpMcpClient(httpClient);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ToolDefinition>> ListToolsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("mcp/tools", cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ToolsListResponse>(
            JsonSerializerOptions.Web,
            cancellationToken);
        return body?.Tools is not { Count: > 0 }
            ? []
            : body.Tools
                .Select(static tool => new ToolDefinition(tool.Name, tool.Description))
                .ToArray();
    }

    /// <inheritdoc />
    public async Task<ToolResult> CallAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            "mcp/tools/call",
            new ToolCallBody(call.Name, call.Arguments),
            JsonSerializerOptions.Web,
            cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<ToolCallPayload>(
            JsonSerializerOptions.Web,
            cancellationToken)
            ?? throw new InvalidOperationException("empty tools/call response");

        var text = payload.Content is { Count: > 0 }
            ? string.Join("\n", payload.Content.Select(static part => part.Text ?? ""))
            : "";

        return payload.IsError || !response.IsSuccessStatusCode
            ? ToolResult.Error(
                string.IsNullOrWhiteSpace(text) ? $"tool {call.Name} failed" : text)
            : ToolResult.Ok(text);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        http.Dispose();
    }

    private sealed record ToolsListResponse(
        [property: JsonPropertyName("tools")] List<ToolInfoDto>? Tools);

    private sealed record ToolInfoDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string? Description);

    private sealed record ToolCallBody(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("arguments")] IReadOnlyDictionary<string, object?> Arguments);

    private sealed record ToolCallPayload(
        [property: JsonPropertyName("content")] List<ToolContentPart>? Content,
        [property: JsonPropertyName("isError")] bool IsError);

    private sealed record ToolContentPart(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("text")] string? Text);
}
