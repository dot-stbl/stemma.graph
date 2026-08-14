using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoints.File;
using Voluta.Graph;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Checkpoints.File.Unit;

public sealed class FileCheckpointerRuntimeShould
{
    [Fact(DisplayName = "Given FileCheckpointer, when interrupt then new FileCheckpointer on same root Resume, then continues to End")]
    public async Task RehydrateAndResumeHitl()
    {
        var root = Path.Combine(Path.GetTempPath(), "voluta-file-rt-" + Guid.NewGuid().ToString("N"));
        try
        {
            var first = new FileCheckpointer(root);
            var graph = BuildInterruptGraph(first);

            var interrupted = await graph.InvokeAsync(
                [],
                new RunOptions { ThreadId = "file-hitl-1", StreamMode = StreamMode.Events });
            interrupted.Kind.ShouldBe(StreamEventKind.Interrupt);

            var reloaded = new FileCheckpointer(root);
            var resumedGraph = BuildInterruptGraph(reloaded);
            var terminal = await resumedGraph.ResumeInvokeAsync(
                "file-hitl-1",
                new Command { Kind = "approve", Payload = "ok" });

            terminal.Kind.ShouldBe(StreamEventKind.End);
            var snapshot = await reloaded.GetAsync("file-hitl-1");
            snapshot!.Status.ShouldBe(GraphRunStatus.Done);
            var messages = snapshot.ChannelValues["messages"].ShouldBeOfType<List<object?>>();
            messages.ShouldContain("approved");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static CompiledGraph BuildInterruptGraph(FileCheckpointer checkpointer)
    {
        return new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "gate",
                static (context, _) => context.ResumePayload is null
                    ? Task.FromResult<NodeResult>(NodeResult.Interrupt("need-approve"))
                    : Task.FromResult<NodeResult>(
                        NodeResult.Continue(new ChannelWrite("messages", "approved"))))
            .AddEdge(GraphConstants.Start, "gate")
            .AddEdge("gate", GraphConstants.End)
            .Compile(checkpointer);
    }
}
