using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Voluta.Samples.MarketingAgent;

/// <summary>
///     Sample-local HTTP client for the MockAdMcp surface
///     (<c>GET /mcp/tools</c>, <c>POST /mcp/tools/call</c>).
/// </summary>
public sealed class MockMcpClient : IDisposable
{
    private readonly HttpClient http;
    private bool disposed;

    private MockMcpClient(HttpClient http)
    {
        this.http = http;
    }

    /// <summary>
    ///     Creates a client for the MockAdMcp HTTP surface.
    /// </summary>
    /// <param name="baseUrl">Base URL (e.g. <c>http://localhost:5190</c>).</param>
    /// <returns>Disposable sample client.</returns>
    public static MockMcpClient Create(string baseUrl)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(15),
        };
        return new MockMcpClient(httpClient);
    }

    /// <summary>
    ///     Lists remote tools.
    /// </summary>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Tool catalog entries.</returns>
    public async Task<IReadOnlyList<McpToolInfo>> ListToolsAsync(
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
                .Select(static tool => new McpToolInfo(tool.Name, tool.Description))
                .ToArray();
    }

    /// <summary>
    ///     Calls a remote tool and returns text; throws on soft error.
    /// </summary>
    /// <param name="name">Tool name.</param>
    /// <param name="arguments">Argument bag.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Result text.</returns>
    public async Task<string> CallAsync(
        string name,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            "mcp/tools/call",
            new ToolCallBody(name, arguments),
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
            ? throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(text) ? $"tool {name} failed" : text)
            : text;
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

/// <summary>
///     Lightweight tool catalog row for CLI listing.
/// </summary>
/// <param name="Name">Tool id.</param>
/// <param name="Description">Optional description.</param>
public sealed record McpToolInfo(string Name, string? Description);
