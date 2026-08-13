# HelloWorld — simulated ReAct

Console sample of a **cyclic agent ⇄ tools** graph. No real LLM or API keys —
the agent node is a pure simulation that requests tools twice, then finishes.

## Run

```bash
dotnet run --project samples/HelloWorld
```

## Graph

```
START → agent ──(status == "tools")──► tools → agent …
              └──(status == "done")───► END
```

Channels:

| Name | Kind | Role |
|------|------|------|
| `messages` | Append | conversation / tool observations |
| `status` | LastValue | router signal (`tools` / `done`) |
| `tool_rounds` | LastValue | how many tool calls have run |

Stream mode is `Updates` — each superstep prints channel writes to the console.
