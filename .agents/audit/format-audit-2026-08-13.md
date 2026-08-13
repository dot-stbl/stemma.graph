# Format / house-rules audit

Date: 2026-08-13
Branch: feature/format-audit @ c9c833f
Files scanned: 72

## Automated greps (pre-format)

| Pattern | Count |
|---------|------:|
| ConfigureAwait | 0 |
| Argument*.ThrowIf | 0 |
| #region | 0 |
| private readonly _field | 0 |
| private method-ish | 22 |

## Largest source files

- **537 lines** · `src\Voluta\Runtime\Engine\RunEngine.cs` (18.8 KB)
- **259 lines** · `src\Voluta.Generators\GraphStateGenerator.cs` (10.7 KB)
- **238 lines** · `src\Voluta.Testing\Conformance\CheckpointerConformance.cs` (10.3 KB)
- **195 lines** · `src\Voluta\Graph\Builder\StateGraph.cs` (8.1 KB)
- **161 lines** · `src\Voluta\Runtime\Engine\ChannelStore.cs` (5.6 KB)
- **129 lines** · `src\Voluta\Graph\CompiledGraph.cs` (5.1 KB)
- **101 lines** · `src\Voluta.Testing\Fixtures\GraphFixtures.cs` (4.4 KB)
- **85 lines** · `src\Voluta\Checkpoint\InMemoryCheckpointer.cs` (3.2 KB)
- **59 lines** · `src\Voluta.Testing\Checkpoint\RecordingCheckpointer.cs` (2.6 KB)
- **62 lines** · `src\Voluta.Testing\Checkpoint\FaultInjectingCheckpointer.cs` (2.4 KB)

## Private method hits (sample)
```
CompiledGraph.cs:113: private static async Task<StreamEvent> DrainToTerminalAsync(
StateGraph.cs:15: private readonly Dictionary<string, ChannelKind> channels = new(StringComparer.Ordinal);
StateGraph.cs:16: private readonly Dictionary<string, NodeHandler> nodes = new(StringComparer.Ordinal);
StateGraph.cs:17: private readonly Dictionary<string, List<string>> staticEdges = new(StringComparer.Ordinal);
StateGraph.cs:186: private static void ValidateEndpoint(
ChannelStore.cs:15: private readonly Dictionary<string, IChannel> channels = new(StringComparer.Ordinal);
ChannelStore.cs:16: private readonly Dictionary<string, long> versions = new(StringComparer.Ordinal);
ChannelStore.cs:17: private readonly Dictionary<string, Dictionary<string, long>> versionsSeen = new(StringComparer.Ordinal);
ChannelStore.cs:139: private void ApplyGrouped(IReadOnlyDictionary<string, List<object?>> grouped)
RunEngine.cs:140: private async IAsyncEnumerable<StreamEvent> RunLoopAsync(
NodeResult.cs:14: private protected NodeResult()
GraphStateGenerator.cs:22: private static readonly SymbolDisplayFormat TypeDisplayFormat = new(
GraphStateGenerator.cs:46: private static void Execute(SourceProductionContext context, INamedTypeSymbol stateType)
GraphStateGenerator.cs:78: private static ImmutableArray<ChannelMember> CollectChannels(
GraphStateGenerator.cs:147: private static string GenerateSource(INamedTypeSymbol stateType, ImmutableArray<ChannelMember> channels)
GraphStateGenerator.cs:244: private static string EscapeString(string value)
GraphStateGenerator.cs:249: private sealed class ChannelMember(
RecordingCheckpointer.cs:13: private readonly ConcurrentQueue<CheckpointSnapshot> puts = new();
RecordingCheckpointer.cs:14: private readonly ConcurrentQueue<CheckpointGetRecord> gets = new();
RecordingCheckpointer.cs:15: private readonly ConcurrentQueue<CheckpointListRecord> lists = new();
CheckpointerConformance.cs:194: private static CheckpointSnapshot CreateSampleSnapshot(
CheckpointerConformance.cs:223: private static void AssertEqual(CheckpointSnapshot expected, CheckpointSnapshot actual)
```

## Structural follow-ups (not format commit)
- Split `RunEngine.cs` if >300 lines after format
- Extract private methods to file-static helpers per class-layout §1a
- Consider regent + CI audit later

## Format plan (B)
1. dotnet format whitespace
2. dotnet format --severity hidden
3. jb cleanupcode Full Cleanup, disable GlobalAll;GlobalPerProduct
4. ban greps + build/test

## Post-format results

### Ran
1. `dotnet format whitespace` — OK
2. `dotnet format --severity hidden` — OK (IDE0130 fixers failed noisily; exit 0)
3. `jb cleanupcode` Full Cleanup — exit 0 with `--no-build` after pre-build; disabled GlobalAll/GlobalPerProduct

### JB Full Cleanup side effects (mitigated)
- Removed required `using` directives from **tests** (Xunit/Shouldly) and some **src** (Concurrent, DI)
- Broke `partial class AgentState` (stripped `partial`)
- Injected stray `using Xunit;` mid-method when repair script failed once

### Mitigation applied
- Restored entire `tests/` tree from HEAD (pre-format)
- Restored missing usings on src from HEAD for Concurrent/DI/etc.
- Re-ran tests green

### Ban greps (final)
- ConfigureAwait: 0
- Argument*.ThrowIf: 0

### Tests
- Abstractions 19 + Unit 19 + Testing 16 + Generators 6 = **60 passed**

### Recommendation
Prefer **dotnet format** as gate. For JB: use **Reformat Code** profile only, or a custom `.sln.DotSettings` that disables Optimize usings / redundant code that drops usings. Full Cleanup is unsafe without a team DotSettings file.

### Structural (unchanged — not in this commit)
- `RunEngine.cs` still ~500+ lines — split later
- private methods still present — extract helpers later
