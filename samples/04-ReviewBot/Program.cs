// 04-ReviewBot — plan → tool gather → review (HITL optional).
//
// Run (offline / no API key):
//   dotnet run --project samples/04-ReviewBot -- --offline --root .
//
// Optional chat:
//   VOLUTA_CHAT_ENDPOINT / VOLUTA_CHAT_API_KEY / VOLUTA_CHAT_MODEL

using Voluta;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Graph.Builder;
using Voluta.Graph.Options;
using Voluta.Samples.Shared;

var offline = HarnessCli.HasFlag(args, "--offline");
var root = HarnessCli.GetOption(args, "--root") ?? ".";
var query = HarnessCli.GetOption(args, "--query") ?? "StateGraph";
var threadId = HarnessCli.GetOption(args, "--thread") ?? $"review-{Guid.NewGuid():N}";
var hitl = HarnessCli.HasFlag(args, "--hitl");

Console.WriteLine("Voluta sample 04 — ReviewBot");
Console.WriteLine($"Thread: {threadId}");
Console.WriteLine($"Root:   {Path.GetFullPath(root)}");
Console.WriteLine($"Query:  {query}");
Console.WriteLine();

SandboxFileSystem sandbox;
try
{
    sandbox = new SandboxFileSystem(root);
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

var chatClient = HarnessCli.CreateChatClient(offline, out var chatLifetime);
using var chatDispose = chatLifetime;

var checkpointer = new InMemoryCheckpointer();
var graph = new StateGraph()
    .AddChannel("plan", ChannelKind.LastValue)
    .AddChannel("evidence", ChannelKind.Append)
    .AddChannel("review", ChannelKind.LastValue)
    .AddChannel("status", ChannelKind.LastValue)
    .AddNode(
        "plan",
        async (context, cancellationToken) =>
        {
            var prompt =
                $"""
                PLAN a short code review checklist for query: {query}
                Sandbox files (sample): {string.Join(", ", sandbox.ListFiles("*.cs").Take(12))}
                """;
            var plan = await chatClient.CompleteAsync(
                "You are a planner for a code-review bot.",
                prompt,
                cancellationToken);
            Console.WriteLine("[plan] done");
            return NodeResult.Continue(
                new ChannelWrite("plan", plan),
                new ChannelWrite("status", "tools"));
        })
    .AddNode(
        "tools",
        (context, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hits = sandbox.Search(query, "*.cs", maxHits: 8);
            Console.WriteLine($"[tools] {hits.Count} hit(s) for '{query}'");
            var writes = new List<ChannelWrite>
            {
                new("status", "review"),
            };
            foreach (var hit in hits)
            {
                var snippet = sandbox.ReadAllText(hit);
                var preview = snippet.Length > 400 ? snippet[..400] + "…" : snippet;
                writes.Add(new ChannelWrite("evidence", $"{hit}:\n{preview}"));
            }

            if (hits.Count == 0)
            {
                writes.Add(new ChannelWrite("evidence", "(no file hits)"));
            }

            return Task.FromResult<NodeResult>(NodeResult.Continue(writes));
        })
    .AddNode(
        "review",
        async (context, cancellationToken) =>
        {
            if (hitl && context.ResumePayload is null)
            {
                Console.WriteLine("[review] interrupt — approve to continue");
                return NodeResult.Interrupt(new { reason = "approve_review", query });
            }

            var plan = context.Read<string>("plan") ?? "";
            var evidence = context.Read<object>("evidence");
            var evidenceText = FormatEvidence(evidence);
            var review = await chatClient.CompleteAsync(
                "You are a reviewer. Produce findings + verdict.",
                $"""
                REVIEW against plan and evidence.
                plan:
                {plan}

                evidence:
                {evidenceText}
                """,
                cancellationToken);
            Console.WriteLine("[review] done");
            return NodeResult.Continue(
                new ChannelWrite("review", review),
                new ChannelWrite("status", "done"));
        })
    .AddEdge(GraphConstants.Start, "plan")
    .AddEdge("plan", "tools")
    .AddEdge("tools", "review")
    .AddEdge("review", GraphConstants.End)
    .Compile(checkpointer, new CompileOptions { RecursionLimit = 16 });

var terminal = await graph.InvokeAsync(
    [new ChannelWrite("status", "start")],
    new RunOptions { ThreadId = threadId, StreamMode = StreamMode.Updates });

if (terminal.Kind == StreamEventKind.Interrupt)
{
    if (!HarnessCli.Confirm("Resume review?"))
    {
        Console.WriteLine("Aborted at interrupt.");
        return 2;
    }

    terminal = await graph.ResumeInvokeAsync(
        threadId,
        new Command { Kind = "approve", Payload = "ok" });
}

var snapshot = await checkpointer.GetAsync(threadId);
Console.WriteLine();
Console.WriteLine($"Status: {snapshot?.Status}");
if (snapshot?.ChannelValues.TryGetValue("review", out var reviewBody) is true)
{
    Console.WriteLine("--- review ---");
    Console.WriteLine(reviewBody);
}

return terminal.Kind is StreamEventKind.Failed ? 1 : 0;

static string FormatEvidence(object? evidence)
{
    return evidence switch
    {
        null => "",
        string text => text,
        System.Collections.IEnumerable list and not string =>
            string.Join("\n---\n", list.Cast<object?>().Select(static item => item?.ToString() ?? "")),
        _ => evidence.ToString() ?? "",
    };
}
