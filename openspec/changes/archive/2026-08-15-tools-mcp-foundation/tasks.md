## 1. Package

- [x] 1.1 Create `src/Voluta.Tools` + PublicAPI files + slnx registration
- [x] 1.2 Implement Tools (`ITool`, `DelegateTool`, `ToolGraphNode`, options)
- [x] 1.3 Implement MCP (`IMcpClient`, `HttpMcpClient`, `McpTool`)

## 2. Tests & samples

- [x] 2.1 Unit tests: fake tool, soft error, timeout, callFactory
- [x] 2.2 Unit tests: HttpMcpClient stub HTTP
- [x] 2.3 MarketingAgent uses product client façade

## 3. Docs & OpenSpec

- [x] 3.1 `docs/0.x/concepts/tools.mdx`
- [x] 3.2 OpenSpec change `tools-mcp-foundation` + delta spec

## 4. Gates

- [x] 4.1 `dotnet build voluta.slnx` + unit tests for Tools
