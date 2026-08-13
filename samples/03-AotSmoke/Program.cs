// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors
//
// 03-AotSmoke — minimal linear graph for Native AOT publish smoke.
//
// Build (JIT / normal):
//   dotnet run --project samples/03-AotSmoke
//
// Publish AOT (requires desktop workload / native toolchain):
//   dotnet publish samples/03-AotSmoke -c Release
//   ./bin/Release/net10.0/<rid>/publish/03-AotSmoke
//
// AOT path: fluent StateGraph + InMemory checkpointer only (no reflection serde).

using StemmaGraph;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Abstractions.Results;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Abstractions.Streaming;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph.Builder;

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

Console.WriteLine("StemmaGraph AOT smoke: ok");
return 0;
