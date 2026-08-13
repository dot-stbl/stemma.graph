// 06-UiHost — minimal WebApplication hosting Voluta.UI (ops console + SSE).
//
// Run:
//   dotnet run --project samples/06-UiHost
// Open:
//   http://localhost:5188/voluta
//
// Seeds one interrupted HITL thread on startup so the queue is non-empty.

using Voluta;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Checkpoint;
using Voluta.Graph;
using Voluta.Graph.Builder;
using Voluta.UI;

const string SeedThreadId = "ui-host-hitl-1";

var checkpointer = new InMemoryCheckpointer();
var graph = new StateGraph()
    .AddChannel("messages", ChannelKind.Append)
    .AddNode("gate", GateNodeAsync)
    .AddEdge(GraphConstants.Start, "gate")
    .AddEdge("gate", GraphConstants.End)
    .Compile(checkpointer);

var session = new VolutaUiSession(graph, checkpointer);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddVolutaUI(session);

var app = builder.Build();
app.MapGet("/", static () => Results.Redirect("/voluta/"));
app.MapVolutaUI(static options => options.PathPrefix = "/voluta");

await SeedInterruptedThreadAsync(session);

app.Logger.LogInformation(
    "Voluta UI host ready. Open {Url} (seed thread {ThreadId})",
    "http://localhost:5188/voluta",
    SeedThreadId);

await app.RunAsync();

static async Task SeedInterruptedThreadAsync(VolutaUiSession session)
{
    await foreach (var _ in session.StreamInvokeAsync(
                       SeedThreadId,
                       [new ChannelWrite("messages", "user: transfer $50")]))
    {
        // Drain until interrupt / end so HITL queue has a row.
    }

    session.TrackThread(SeedThreadId);
}

static Task<NodeResult> GateNodeAsync(GraphContext context, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    return context.ResumePayload is null
        ? Task.FromResult<NodeResult>(
            NodeResult.Interrupt(new { action = "transfer", amount = 50, currency = "USD" }))
        : Task.FromResult<NodeResult>(
            NodeResult.Continue(new ChannelWrite("messages", "gate: transfer approved")));
}
