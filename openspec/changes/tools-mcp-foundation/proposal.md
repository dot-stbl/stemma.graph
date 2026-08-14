## Why

Tool-calling agents need a stable invoke → channel-write seam without Voluta
owning an LLM SDK. Patterns lived only in `samples/MockAdMcp` +
`MarketingAgent.MockMcpClient`. Milestone v0.4 wants product batteries
(GitHub #72).

## What Changes

- New package **`Voluta.Tools`**: `ITool`, `DelegateTool`, `ToolGraphNode`
  (timeout + channel writes), `IMcpClient` / `HttpMcpClient` / `McpTool`.
- Docs cookbook: `docs/0.x/concepts/tools.mdx`.
- MarketingAgent uses product `HttpMcpClient` under a thin sample façade.
- Unit tests with fake tools + stub HTTP MCP.

## Non-goals

- Full LangChain tools port
- New LLM client / MEAI tool schema generation
- Full MCP transports (stdio, SSE sessions, resources)

## Capabilities

### New Capabilities

- `tools-mcp`: tool node conventions + light MCP HTTP client adapter

### Modified Capabilities

_(none)_

## Impact

- `src/Voluta.Tools/**`, `tests/Voluta.Tools.Unit/**`
- `samples/MarketingAgent` ProjectReference + façade
- `docs/0.x/concepts/tools.mdx`
- Architecture isolation lists include `Voluta.Tools`
