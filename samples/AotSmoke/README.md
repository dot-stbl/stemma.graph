# AotSmoke

Minimal linear graph used to validate **Native AOT** publish of the core runtime
(`Voluta` + `InMemoryCheckpointer`, no reflection-based checkpoint serde).

## Run (JIT)

```bash
dotnet run --project samples/AotSmoke
```

## Publish Native AOT

Requires a native toolchain (Windows: Desktop development with C++; Linux: clang).

```bash
dotnet publish samples/AotSmoke -c Release
```

The project sets `PublishAot=true` by default for this sample only.

## Scope (two-tier product model)

| AOT core (this sample) | Full .NET runtime packages |
|------------------------|----------------------------|
| `Voluta` + Abstractions | `Voluta.Checkpoints.*` (EF/S3/File) |
| InMemory checkpointer | `Voluta.UI.*`, Agents.AI |
| Optional: DependencyInjection | ASP.NET hosts, reflection-heavy adapters |
| Fluent graph only | Dynamic plugin load |
