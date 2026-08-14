using Voluta.Tools.Mcp;
using Voluta.Tools.Tools;

namespace Voluta.Samples.MarketingAgent;

/// <summary>
///     Sample-facing façade over <see cref="HttpMcpClient" /> (product package).
///     Keeps the harness API as string results that throw on soft tool errors.
/// </summary>
public sealed class MockMcpClient : IDisposable
{
    private readonly HttpMcpClient inner;
    private bool disposed;

    private MockMcpClient(HttpMcpClient inner)
    {
        this.inner = inner;
    }

    /// <summary>
    ///     Creates a client for the MockAdMcp HTTP surface.
    /// </summary>
    /// <param name="baseUrl">Base URL (e.g. <c>http://localhost:5190</c>).</param>
    /// <returns>Disposable sample client.</returns>
    public static MockMcpClient Create(string baseUrl)
    {
        return new MockMcpClient(HttpMcpClient.Create(baseUrl));
    }

    /// <summary>
    ///     Lists remote tools.
    /// </summary>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Tool catalog entries.</returns>
    public async Task<IReadOnlyList<McpToolInfo>> ListToolsAsync(
        CancellationToken cancellationToken = default)
    {
        var tools = await inner.ListToolsAsync(cancellationToken);
        return tools
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
        var result = await inner.CallAsync(new ToolCall(name, arguments), cancellationToken);
        return result.IsError
            ? throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(result.Text) ? $"tool {name} failed" : result.Text)
            : result.Text;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        inner.Dispose();
    }
}

/// <summary>
///     Lightweight tool catalog row for CLI listing.
/// </summary>
/// <param name="Name">Tool id.</param>
/// <param name="Description">Optional description.</param>
public sealed record McpToolInfo(string Name, string? Description);
