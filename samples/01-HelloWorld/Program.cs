// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors
//
// 01-HelloWorld — simulated ReAct loop (agent ⇄ tools) without a real LLM.
//
// Run:
//   dotnet run --project samples/01-HelloWorld
//
// Graph:
//   START → agent ──(status==tools)──► tools → agent …
//                 └──(status==done)──► END
//
// The agent is pure simulation: after two tool rounds it finishes.

using StemmaGraph;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph;
using StemmaGraph.Abstractions.Results;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Abstractions.Streaming;
using StemmaGraph.Graph.Builder;
using StemmaGraph.Graph.Options;

const string ThreadId = "react-sample-1";

var checkpointer = new InMemoryCheckpointer();
var graph = new StateGraph()
    .AddChannel("messages", ChannelKind.Append)
    .AddChannel("status", ChannelKind.LastValue)
    .AddChannel("tool_rounds", ChannelKind.LastValue)
    .AddNode("agent", AgentNodeAsync)
    .AddNode("tools", ToolsNodeAsync)
    .AddEdge(GraphConstants.Start, "agent")
    .AddConditionalEdges(
        "agent",
        static context => context.Read<string>("status") == "tools" ? "tools" : GraphConstants.End)
    .AddEdge("tools", "agent")
    .Compile(checkpointer, new CompileOptions { RecursionLimit = 32 });

Console.WriteLine("StemmaGraph sample 01 — simulated ReAct (agent ⇄ tools)");
Console.WriteLine($"Thread: {ThreadId}");
Console.WriteLine();

var input = new ChannelWrite[]
{
    new("messages", "user: what's the weather in Oslo?"),
    new("status", "start"),
    new("tool_rounds", 0),
};

await foreach (var item in graph.StreamAsync(
                   input,
                   new RunOptions { ThreadId = ThreadId, StreamMode = StreamMode.Updates }))
{
    PrintEvent(item);
}

var snapshot = await checkpointer.GetAsync(ThreadId);
Console.WriteLine();
Console.WriteLine($"Final status: {snapshot?.Status}");
if (snapshot?.ChannelValues.TryGetValue("messages", out var messages) is true)
{
    Console.WriteLine("Messages:");
    if (messages is System.Collections.IEnumerable list and not string)
    {
        foreach (var message in list)
        {
            Console.WriteLine($"  - {message}");
        }
    }
}

return 0;

static Task<NodeResult> AgentNodeAsync(GraphContext context, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    var rounds = context.Read<int?>("tool_rounds") ?? 0;
    if (rounds < 2)
    {
        Console.WriteLine($"[agent] round {rounds}: requesting tools");
        return Task.FromResult<NodeResult>(
            NodeResult.Continue(
                new ChannelWrite("status", "tools"),
                new ChannelWrite("messages", $"agent: call get_weather (round {rounds + 1})")));
    }

    Console.WriteLine("[agent] enough tool data — finishing");
    return Task.FromResult<NodeResult>(
        NodeResult.Continue(
            new ChannelWrite("status", "done"),
            new ChannelWrite("messages", "agent: final answer — cloudy, 12°C in Oslo")));
}

static Task<NodeResult> ToolsNodeAsync(GraphContext context, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    var rounds = (context.Read<int?>("tool_rounds") ?? 0) + 1;
    Console.WriteLine($"[tools] executing simulated tool (round {rounds})");
    return Task.FromResult<NodeResult>(
        NodeResult.Continue(
            new ChannelWrite("tool_rounds", rounds),
            new ChannelWrite("messages", $"tools: observation — temp=12C (round {rounds})"),
            new ChannelWrite("status", "agent")));
}

static void PrintEvent(StreamEvent item)
{
    var nodes = item.NodeNames.Count == 0 ? "-" : string.Join(",", item.NodeNames);
    Console.WriteLine($"stream step={item.Step} kind={item.Kind} nodes=[{nodes}]");
    foreach (var write in item.Writes)
    {
        Console.WriteLine($"  write {write.ChannelName} = {FormatValue(write.Value)}");
    }

    if (item.Payload is not null)
    {
        Console.WriteLine($"  payload = {item.Payload}");
    }
}

static string FormatValue(object? value)
{
    return value switch
    {
        null => "null",
        string text => text,
        System.Collections.IEnumerable enumerable and not string =>
            "[" + string.Join(", ", enumerable.Cast<object?>().Select(static item => item?.ToString() ?? "null")) + "]",
        _ => value.ToString() ?? "null",
    };
}
