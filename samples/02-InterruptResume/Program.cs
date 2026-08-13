// 02-InterruptResume — HITL interrupt + Command approve resume.
//
// Run:
//   dotnet run --project samples/02-InterruptResume
//
// Graph:
//   START → gate → END
//
// First visit to `gate` interrupts with a payload. The host prints the
// interrupt, then ResumeAsync(Command { Kind = "approve" }) continues the run.

using StemmaGraph;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph;
using StemmaGraph.Abstractions.Results;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Abstractions.Streaming;
using StemmaGraph.Graph.Builder;

const string ThreadId = "hitl-sample-1";

var checkpointer = new InMemoryCheckpointer();
var graph = new StateGraph()
    .AddChannel("messages", ChannelKind.Append)
    .AddNode("gate", GateNodeAsync)
    .AddEdge(GraphConstants.Start, "gate")
    .AddEdge("gate", GraphConstants.End)
    .Compile(checkpointer);

Console.WriteLine("StemmaGraph sample 02 — interrupt / resume (HITL)");
Console.WriteLine($"Thread: {ThreadId}");
Console.WriteLine();

Console.WriteLine("=== Invoke (expect interrupt) ===");
await foreach (var item in graph.StreamAsync(
                   [new ChannelWrite("messages", "user: transfer $50")],
                   new RunOptions { ThreadId = ThreadId, StreamMode = StreamMode.Events }))
{
    PrintEvent(item);
}

var interrupted = await checkpointer.GetAsync(ThreadId);
Console.WriteLine();
Console.WriteLine($"Checkpoint status after invoke: {interrupted?.Status}");
Console.WriteLine($"Interrupt payload: {FormatValue(interrupted?.InterruptPayload)}");

if (interrupted?.Status != GraphRunStatus.Interrupted)
{
    Console.Error.WriteLine("Expected Interrupted status — aborting sample.");
    return 1;
}

Console.WriteLine();
Console.WriteLine("=== Resume with Command.Kind = approve ===");
await foreach (var item in graph.ResumeAsync(
                   ThreadId,
                   new Command { Kind = "approve", Payload = "ok" },
                   StreamMode.Events))
{
    PrintEvent(item);
}

var done = await checkpointer.GetAsync(ThreadId);
Console.WriteLine();
Console.WriteLine($"Checkpoint status after resume: {done?.Status}");
if (done?.ChannelValues.TryGetValue("messages", out var messages) is true)
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

return done?.Status == GraphRunStatus.Done ? 0 : 2;

static Task<NodeResult> GateNodeAsync(GraphContext context, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    if (context.ResumePayload is null)
    {
        Console.WriteLine("[gate] interrupting for human approval");
        return Task.FromResult<NodeResult>(
            NodeResult.Interrupt(new { action = "transfer", amount = 50, currency = "USD" }));
    }

    Console.WriteLine($"[gate] resumed with payload={FormatValue(context.ResumePayload)} — approving");
    return Task.FromResult<NodeResult>(
        NodeResult.Continue(new ChannelWrite("messages", "gate: transfer approved")));
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
        Console.WriteLine($"  payload = {FormatValue(item.Payload)}");
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
