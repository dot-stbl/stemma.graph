// DocQ — search sandbox markdown/docs → answer with citations.
//
// Run:
//   dotnet run --project samples/DocQ -- --offline --root . --question "What is Voluta?"

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
var question = HarnessCli.GetOption(args, "--question")
               ?? HarnessCli.GetOption(args, "-q")
               ?? "What is Voluta?";
var threadId = HarnessCli.GetOption(args, "--thread") ?? $"docq-{Guid.NewGuid():N}";

CliUi.Banner(
    "DocQ",
    "search sandbox → answer with citations",
    ("thread", threadId),
    ("root", Path.GetFullPath(root)),
    ("question", question),
    ("mode", offline ? "offline" : "live"));

SandboxFileSystem sandbox;
try
{
    sandbox = new SandboxFileSystem(root);
}
catch (Exception exception)
{
    CliUi.Error(exception.Message);
    return 1;
}

var chatClient = HarnessCli.CreateChatClient(offline, out var chatLifetime);
using var chatDispose = chatLifetime;

var checkpointer = new InMemoryCheckpointer();
var graph = new StateGraph()
    .AddChannel("question", ChannelKind.LastValue)
    .AddChannel("sources", ChannelKind.Append)
    .AddChannel("answer", ChannelKind.LastValue)
    .AddNode(
        "search",
        (context, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var q = context.Read<string>("question") ?? question;
            var terms = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static term => term.Length > 3)
                .Take(4)
                .ToArray();
            if (terms.Length == 0)
            {
                terms = [q];
            }

            var hits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var term in terms)
            {
                foreach (var path in sandbox.Search(term, "*.md", maxHits: 10))
                {
                    hits.Add(path);
                }

                foreach (var path in sandbox.Search(term, "*.cs", maxHits: 5))
                {
                    hits.Add(path);
                }
            }

            CliUi.Node("search", $"{hits.Count} source(s)");
            var writes = new List<ChannelWrite>();
            foreach (var path in hits.Take(12))
            {
                var body = sandbox.ReadAllText(path);
                var preview = body.Length > 600 ? body[..600] + "…" : body;
                writes.Add(new ChannelWrite("sources", $"{path}:\n{preview}"));
            }

            if (hits.Count == 0)
            {
                writes.Add(new ChannelWrite("sources", "(no hits)"));
            }

            return Task.FromResult<NodeResult>(NodeResult.Continue(writes));
        })
    .AddNode(
        "answer",
        async (context, cancellationToken) =>
        {
            var q = context.Read<string>("question") ?? question;
            var sources = FormatSources(context.Read<object>("sources"));
            var answer = await chatClient.CompleteAsync(
                "You answer documentation questions. Cite sources by path.",
                $"""
                QUESTION: {q}

                SOURCES:
                {sources}
                """,
                cancellationToken);
            CliUi.Node("answer", "drafted");
            return NodeResult.Continue(new ChannelWrite("answer", answer));
        })
    .AddEdge(GraphConstants.Start, "search")
    .AddEdge("search", "answer")
    .AddEdge("answer", GraphConstants.End)
    .Compile(checkpointer, new CompileOptions { RecursionLimit = 8 });

var terminal = await graph.InvokeAsync(
    [new ChannelWrite("question", question)],
    new RunOptions { ThreadId = threadId, StreamMode = StreamMode.Updates });

var snapshot = await checkpointer.GetAsync(threadId);
CliUi.Section("result");
CliUi.KeyValue("status", snapshot?.Status.ToString());
if (snapshot?.ChannelValues.TryGetValue("answer", out var answerBody) is true)
{
    CliUi.Panel("answer", answerBody?.ToString() ?? "");
}

if (terminal.Kind is StreamEventKind.Failed)
{
    CliUi.Error("run failed");
    return 1;
}

CliUi.Ok("done");
return 0;

static string FormatSources(object? sources)
{
    return sources switch
    {
        null => "",
        string text => text,
        System.Collections.IEnumerable list and not string =>
            string.Join("\n---\n", list.Cast<object?>().Select(static item => item?.ToString() ?? "")),
        _ => sources.ToString() ?? "",
    };
}
