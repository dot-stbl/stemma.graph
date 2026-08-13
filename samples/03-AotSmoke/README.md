# 03-AotSmoke

Minimal linear graph used to validate **Native AOT** publish of the core runtime
(`StemmaGraph` + `InMemoryCheckpointer`, no reflection-based checkpoint serde).

## Run (JIT)

```bash
dotnet run --project samples/03-AotSmoke
```

## Publish Native AOT

Requires a native toolchain (Windows: Desktop development with C++; Linux: clang).

```bash
dotnet publish samples/03-AotSmoke -c Release
```

The project sets `PublishAot=true` by default for this sample only.

## Scope (two-tier product model)

| AOT core (this sample) | Full .NET runtime packages |
|------------------------|----------------------------|
| `StemmaGraph` + Abstractions | `StemmaGraph.Checkpoints.*` (EF/S3/File) |
| InMemory checkpointer | `StemmaGraph.UI.*`, MicrosoftAi |
| Optional: DependencyInjection | ASP.NET hosts, reflection-heavy adapters |
| Fluent graph only | Dynamic plugin load |
