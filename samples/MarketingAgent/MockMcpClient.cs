using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Voluta.Samples.MarketingAgent;

/// <summary>
///     Thin HTTP client for the demo MockAdMcp tools surface.
/// </summary>
public sealed class MockMcpClient(HttpClient http) : IDisposable
{
    private bool disposed;

    public static MockMcpClient Create(string baseUrl)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(15),
        };
        return new MockMcpClient(httpClient);
    }

    public async Task<IReadOnlyList<McpToolInfo>> ListToolsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("mcp/tools", cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ToolsListResponse>(
            JsonSerializerOptions.Web,
            cancellationToken);
        return body?.Tools ?? [];
    }

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
        var payload = await response.Content.ReadFromJsonAsync<ToolCallResult>(
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

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        http.Dispose();
    }
}

public sealed record McpToolInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description);

internal sealed record ToolsListResponse(
    [property: JsonPropertyName("tools")] List<McpToolInfo>? Tools);

internal sealed record ToolCallBody(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("arguments")] IReadOnlyDictionary<string, object?> Arguments);

internal sealed record ToolCallResult(
    [property: JsonPropertyName("content")] List<ToolContentPart>? Content,
    [property: JsonPropertyName("isError")] bool IsError);

internal sealed record ToolContentPart(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("text")] string? Text);
