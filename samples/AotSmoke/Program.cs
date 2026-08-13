// AotSmoke — minimal linear graph for Native AOT publish smoke.
//
// Build (JIT / normal):
//   dotnet run --project samples/AotSmoke
//
// Publish AOT (requires desktop workload / native toolchain):
//   dotnet publish samples/AotSmoke -c Release
//   ./bin/Release/net10.0/<rid>/publish/AotSmoke
//
// AOT path: fluent StateGraph + InMemory checkpointer only (no reflection serde).

using Voluta;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Graph.Builder;

var graph = new StateGraph()
    .AddChannel("value", ChannelKind.LastValue)
    .AddNode(
        "set",
        static async (_, _) =>
        {
            await Task.CompletedTask;
            return NodeResult.Continue(new ChannelWrite("value", "aot-ok"));
        })
    .AddEdge(GraphConstants.Start, "set")
    .AddEdge("set", GraphConstants.End)
    .Compile(new InMemoryCheckpointer());

var terminal = await graph.InvokeAsync(
    [new ChannelWrite("value", "seed")],
    new RunOptions { ThreadId = "aot-smoke", StreamMode = StreamMode.Values });

if (terminal.Kind is not StreamEventKind.End)
{
    Console.Error.WriteLine($"AOT smoke failed: {terminal.Kind}");
    return 1;
}

var value = terminal.State?["value"]?.ToString();
if (value is not "aot-ok")
{
    Console.Error.WriteLine($"AOT smoke unexpected value: {value}");
    return 2;
}

Console.WriteLine("Voluta AOT smoke: ok");
return 0;
