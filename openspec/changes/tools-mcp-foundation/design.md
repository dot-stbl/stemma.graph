## Context

Agents.AI already adapts MAF/`IChatClient` as `IGraphNode`. Tools are orthogonal:
any node may call external work. Productizing MockAdMcp’s HTTP shape gives a
single client for samples and hosts without pulling AI packages into core.

## Decisions

1. **Separate package `Voluta.Tools`** (not under Agents.AI) — no MEAI/MAF deps;
   AOT-optional; mirrors Checkpoints.* / OpenTelemetry packaging.
2. **`ITool` is the unit** — `ToolGraphNode` only knows invoke + write channels.
3. **Timeout at the node** via linked CTS → `ToolInvocationException` (not a
   silent soft error).
4. **Soft MCP errors** map to `ToolResult.IsError`; optional `ThrowOnError` /
   `ErrorChannel`.
5. **`IMcpClient` is transport-shaped**, not protocol-complete — HTTP demo first;
   real MCP later implements the same interface.

## Risks / trade-offs

- HTTP MCP is demo-shaped; hosts must not assume full MCP compliance.
- `HttpMcpClient.Create` owns an `HttpClient` (same trade-off as the old sample
  client). Prefer injecting `HttpClient` in production hosts.
