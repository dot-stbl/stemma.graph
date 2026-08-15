# Tools Mcp Specification (delta)

## Purpose

Defines the public tool-calling batteries package: invoke a named tool from a
graph node, write results to channels, optional timeout, and a light MCP-shaped
HTTP client adapter.

## ADDED Requirements

### Requirement: Tool abstraction without LLM SDK
The product MUST expose an `ITool` with stable `ToolDefinition` metadata and
`InvokeAsync(ToolCall, CancellationToken)` returning `ToolResult` text + soft
error flag. Consumers MUST be able to implement tools with a delegate without
referencing Microsoft.Extensions.AI or Microsoft.Agents.AI.

#### Scenario: Delegate tool succeeds
- **WHEN** a `DelegateTool` returns `ToolResult.Ok(text)`
- **THEN** a `ToolGraphNode` writes that text to the configured output channel

#### Scenario: Soft error without throw
- **WHEN** a tool returns `ToolResult.Error(message)` and `ThrowOnError` is false
- **THEN** the node continues and writes the message (and optional error channel)

### Requirement: Tool graph node with timeout
A graph node helper MUST invoke a tool, honor cancellation, and optionally apply
a wall-clock timeout. Timeout MUST surface as a typed tool invocation failure
when the graph token is not cancelled.

#### Scenario: Timeout
- **WHEN** the tool does not complete within `ToolNodeOptions.Timeout`
- **THEN** invocation fails with `ToolInvocationException` naming the tool

### Requirement: MCP-shaped HTTP client
The product MUST provide an `IMcpClient` with `ListToolsAsync` and `CallAsync`,
and an `HttpMcpClient` implementation for the demo HTTP surface
(`GET /mcp/tools`, `POST /mcp/tools/call`).

#### Scenario: List and call
- **WHEN** a host lists tools then calls a tool by name with arguments
- **THEN** the client maps the JSON catalog and content payload into
  `ToolDefinition` / `ToolResult` without leaking raw transport DTOs to nodes

### Requirement: MCP tool as ITool
Consumers MUST be able to wrap an `IMcpClient` + tool name as an `ITool` for use
with `ToolGraphNode`.

#### Scenario: McpTool node
- **WHEN** an `McpTool` is registered as a `ToolGraphNode`
- **THEN** node invocation forwards to `IMcpClient.CallAsync` with the definition name

### Requirement: Package isolation
`Voluta.Tools` MUST depend on the core runtime package and MUST NOT be referenced
by `Voluta` or `Voluta.Abstractions`. Architecture tests MUST list Tools among
packages that core may not reference.

#### Scenario: Core stays free of Tools
- **WHEN** architecture package isolation tests run
- **THEN** `Voluta` project references exclude `Voluta.Tools`
