// HelloWorld — simulated ReAct loop (agent ⇄ tools) without a real LLM.
//
// Run:
//   dotnet run --project samples/HelloWorld
//
// Graph:
//   START → agent ──(status==tools)──► tools → agent …
//                 └──(status==done)──► END
//
// The agent is pure simulation: after two tool rounds it finishes.

using Voluta;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Graph;
using Voluta.Graph.Builder;
using Voluta.Graph.Options;
using Voluta.Samples.Shared;

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

CliUi.Banner(
    "HelloWorld",
    "simulated ReAct · agent ⇄ tools · no LLM",
    ("thread", ThreadId),
    ("stream", "Updates"));

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
    CliUi.StreamEvent(item);
}

var snapshot = await checkpointer.GetAsync(ThreadId);
CliUi.Section("result");
CliUi.KeyValue("status", snapshot?.Status.ToString());
if (snapshot?.ChannelValues.TryGetValue("messages", out var messages) is true)
{
    CliUi.Messages(messages);
}

CliUi.Ok("done");
return 0;

static Task<NodeResult> AgentNodeAsync(GraphContext context, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    var rounds = context.Read<int?>("tool_rounds") ?? 0;
    if (rounds < 2)
    {
        CliUi.Node("agent", $"round {rounds} · requesting tools");
        return Task.FromResult<NodeResult>(
            NodeResult.Continue(
                new ChannelWrite("status", "tools"),
                new ChannelWrite("messages", $"agent: call get_weather (round {rounds + 1})")));
    }

    CliUi.Node("agent", "enough tool data · finishing");
    return Task.FromResult<NodeResult>(
        NodeResult.Continue(
            new ChannelWrite("status", "done"),
            new ChannelWrite("messages", "agent: final answer — cloudy, 12°C in Oslo")));
}

static Task<NodeResult> ToolsNodeAsync(GraphContext context, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    var rounds = (context.Read<int?>("tool_rounds") ?? 0) + 1;
    CliUi.Node("tools", $"executing simulated tool · round {rounds}");
    return Task.FromResult<NodeResult>(
        NodeResult.Continue(
            new ChannelWrite("tool_rounds", rounds),
            new ChannelWrite("messages", $"tools: observation — temp=12C (round {rounds})"),
            new ChannelWrite("status", "agent")));
}
