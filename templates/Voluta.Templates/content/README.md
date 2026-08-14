# VolutaAgent

Scaffold from **`dotnet new voluta-agent`**. A small console host that:

1. Registers Voluta with **`AddVoluta`** + **InMemory** checkpoints  
2. Runs **START → intake → gate → chat → END**  
3. **Interrupts** on `gate` for human approval, then **`Command.Approve`**  
4. Calls a **MEAI `IChatClient`** node (`Voluta.Agents.AI`) — **offline stub by default** (no API key)

## Run

```bash
dotnet run
```

Expected flow:

1. Stream until `Interrupted` on `gate`  
2. Automatic resume with `Command.Approve("ok")`  
3. Offline chat writes an answer channel; status `Done`

## Layout

| File | Role |
|------|------|
| `Program.cs` | Host, graph, HITL demo loop, `OfflineChatClient` |
| `VolutaAgent.csproj` | `Voluta` + `Voluta.DependencyInjection` + `Voluta.Agents.AI` |

## Swap checkpoint to disk

```bash
dotnet add package Voluta.Checkpoints.File
```

```csharp
using Voluta.Checkpoints.File;

// inside AddVoluta:
voluta.Checkpoints.UseFile("./.voluta/checkpoints");
// instead of UseInMemory()
```

## Optional live chat

`OfflineChatClient` implements `IChatClient` so the project **always builds**.
To use a real model, register your provider client instead:

```csharp
// example sketch — pick your MEAI provider package
builder.Services.AddSingleton<IChatClient>(_ => /* OpenAI / Azure / Ollama client */);
```

Remove or stop registering `OfflineChatClient` when you go live.

## In-repo development

If you are working on Voluta itself, replace the NuGet `PackageReference`s in
the csproj with `ProjectReference`s to `src/Voluta`, `src/Voluta.DependencyInjection`,
and `src/Voluta.Agents.AI` (see comments in the csproj).

## Learn more

- [Install](https://docs.stbl.space/voluta/0.x/install) — packages + template install  
- [Quick Start](https://docs.stbl.space/voluta/0.x/quick-start) — ReAct sample walkthrough  
- [Interrupts](https://docs.stbl.space/voluta/0.x/concepts/interrupts) — HITL model  
- Repo samples: `samples/InterruptResume`, `samples/HelloWorld`, `samples/WorkerHost`
