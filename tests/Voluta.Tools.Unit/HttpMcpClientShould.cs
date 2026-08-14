using System.Net;
using System.Text;
using Shouldly;
using Voluta.Tools.Mcp;
using Voluta.Tools.Tools;
using Xunit;

namespace Voluta.Tools.Unit;

public sealed class HttpMcpClientShould
{
    [Fact(DisplayName = "Given list tools JSON, when ListToolsAsync, then maps catalog entries")]
    public async Task ListToolsFromHttpSurface()
    {
        using var http = CreateClient(
            HttpMethod.Get,
            "mcp/tools",
            """{"tools":[{"name":"list_ssps","description":"SSP catalog"}]}""");
        using var client = new HttpMcpClient(http);

        var tools = await client.ListToolsAsync();

        tools.Count.ShouldBe(1);
        tools[0].Name.ShouldBe("list_ssps");
        tools[0].Description.ShouldBe("SSP catalog");
    }

    [Fact(DisplayName = "Given tools/call success, when CallAsync, then returns Ok text")]
    public async Task CallToolSuccess()
    {
        using var http = CreateClient(
            HttpMethod.Post,
            "mcp/tools/call",
            """{"content":[{"type":"text","text":"{\"ok\":true}"}],"isError":false}""");
        using var client = new HttpMcpClient(http);

        var result = await client.CallAsync(new ToolCall("list_ssps"));

        result.IsError.ShouldBeFalse();
        result.Text.ShouldContain("ok");
    }

    [Fact(DisplayName = "Given tools/call isError, when CallAsync, then returns soft error")]
    public async Task CallToolSoftError()
    {
        using var http = CreateClient(
            HttpMethod.Post,
            "mcp/tools/call",
            """{"content":[{"type":"text","text":"unknown tool"}],"isError":true}""");
        using var client = new HttpMcpClient(http);

        var result = await client.CallAsync(new ToolCall("missing"));

        result.IsError.ShouldBeTrue();
        result.Text.ShouldBe("unknown tool");
    }

    [Fact(DisplayName = "Given McpTool, when InvokeAsync, then forwards to client with definition name")]
    public async Task McpToolForwardsToClient()
    {
        using var http = CreateClient(
            HttpMethod.Post,
            "mcp/tools/call",
            """{"content":[{"type":"text","text":"from-mcp"}],"isError":false}""");
        using var client = new HttpMcpClient(http);
        var tool = McpTool.Create(client, "list_ssps", "SSP catalog");

        var result = await tool.InvokeAsync(new ToolCall("list_ssps"));

        result.Text.ShouldBe("from-mcp");
        tool.Definition.Name.ShouldBe("list_ssps");
    }

    private static HttpClient CreateClient(HttpMethod method, string path, string jsonBody)
    {
        var handler = new StubHttpHandler(method, path, jsonBody);
        return new HttpClient(handler) { BaseAddress = new Uri("http://mcp.test/") };
    }

    private sealed class StubHttpHandler(HttpMethod expectedMethod, string expectedPath, string jsonBody)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath.TrimStart('/') ?? "";
            return request.Method != expectedMethod
                   || !string.Equals(path, expectedPath, StringComparison.Ordinal)
                ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))
                : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
                });
        }
    }
}
