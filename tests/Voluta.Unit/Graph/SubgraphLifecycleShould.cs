using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Graph;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Graph;

public sealed class SubgraphLifecycleShould
{
    [Fact(DisplayName = "Given child interrupt, when parent resume, then child completes and parent continues")]
    public async Task ChildInterruptThenParentResumeCompletesChild()
    {
        var sharedCheckpointer = new InMemoryCheckpointer();
        var child = BuildChildWithGate(sharedCheckpointer);
        var parent = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddChannel("child_out", ChannelKind.LastValue)
            .AddNode(
                "before",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "before"))))
            .AddNode(
                "nested",
                Subgraph.AsNode(
                    child,
                    inputChannels: [],
                    outputChannels: ["child_out"]))
            .AddNode(
                "after",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "after"))))
            .AddEdge(GraphConstants.Start, "before")
            .AddEdge("before", "nested")
            .AddEdge("nested", "after")
            .AddEdge("after", GraphConstants.End)
            .Compile(sharedCheckpointer);

        var interrupted = await parent.InvokeAsync(
            [],
            new RunOptions { ThreadId = "parent-1", StreamMode = StreamMode.Events });

        interrupted.Kind.ShouldBe(StreamEventKind.Interrupt);
        interrupted.NodeNames.ShouldBe(["nested"]);
        interrupted.Payload.ShouldNotBeNull();

        var parentSnapshot = await sharedCheckpointer.GetAsync("parent-1");
        parentSnapshot!.Status.ShouldBe(GraphRunStatus.Interrupted);
        parentSnapshot.NextNodes.ShouldBe(["nested"]);
        parentSnapshot.LastNode.ShouldBe("nested");

        var childSnapshot = await sharedCheckpointer.GetAsync("parent-1/nested");
        childSnapshot.ShouldNotBeNull();
        childSnapshot!.Status.ShouldBe(GraphRunStatus.Interrupted);
        childSnapshot.NextNodes.ShouldBe(["gate"]);

        var terminal = await parent.ResumeInvokeAsync(
            "parent-1",
            Command.Approve("signed-off"));

        terminal.Kind.ShouldBe(StreamEventKind.End);

        var parentDone = await sharedCheckpointer.GetAsync("parent-1");
        parentDone!.Status.ShouldBe(GraphRunStatus.Done);
        var messages = parentDone.ChannelValues["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldContain("before");
        messages.ShouldContain("after");
        parentDone.ChannelValues["child_out"].ShouldBe("child-done");

        var childDone = await sharedCheckpointer.GetAsync("parent-1/nested");
        childDone!.Status.ShouldBe(GraphRunStatus.Done);
    }

    [Fact(DisplayName = "Given custom threadIdFactory, when nested runs, then child uses factory thread id")]
    public async Task CustomThreadIdFactoryNamespacesChild()
    {
        var sharedCheckpointer = new InMemoryCheckpointer();
        var child = BuildChildWithGate(sharedCheckpointer);
        string? capturedParentThread = null;
        string? capturedNodeName = null;

        var parent = new StateGraph()
            .AddChannel("child_out", ChannelKind.LastValue)
            .AddNode(
                "agent",
                Subgraph.AsNode(
                    child,
                    inputChannels: [],
                    outputChannels: ["child_out"],
                    threadIdFactory: context =>
                    {
                        capturedParentThread = context.ThreadId;
                        capturedNodeName = context.NodeName;
                        return $"nest::{context.ThreadId}::{context.NodeName}";
                    }))
            .AddEdge(GraphConstants.Start, "agent")
            .AddEdge("agent", GraphConstants.End)
            .Compile(sharedCheckpointer);

        var interrupted = await parent.InvokeAsync(
            [],
            new RunOptions { ThreadId = "host-a", StreamMode = StreamMode.Events });
        interrupted.Kind.ShouldBe(StreamEventKind.Interrupt);

        capturedParentThread.ShouldBe("host-a");
        capturedNodeName.ShouldBe("agent");

        var childSnapshot = await sharedCheckpointer.GetAsync("nest::host-a::agent");
        childSnapshot.ShouldNotBeNull();
        childSnapshot!.Status.ShouldBe(GraphRunStatus.Interrupted);

        var terminal = await parent.ResumeInvokeAsync("host-a", Command.Approve("ok"));
        terminal.Kind.ShouldBe(StreamEventKind.End);

        var childDone = await sharedCheckpointer.GetAsync("nest::host-a::agent");
        childDone!.Status.ShouldBe(GraphRunStatus.Done);
    }

    [Fact(DisplayName = "Given happy-path child, when parent invokes, then maps child outputs without interrupt")]
    public async Task HappyPathMapsOutputs()
    {
        var sharedCheckpointer = new InMemoryCheckpointer();
        var child = new StateGraph()
            .AddChannel("child_out", ChannelKind.LastValue)
            .AddNode(
                "work",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("child_out", "ok"))))
            .AddEdge(GraphConstants.Start, "work")
            .AddEdge("work", GraphConstants.End)
            .Compile(sharedCheckpointer);

        var parent = new StateGraph()
            .AddChannel("child_out", ChannelKind.LastValue)
            .AddNode(
                "nested",
                Subgraph.AsNode(child, inputChannels: [], outputChannels: ["child_out"]))
            .AddEdge(GraphConstants.Start, "nested")
            .AddEdge("nested", GraphConstants.End)
            .Compile(sharedCheckpointer);

        var terminal = await parent.InvokeAsync(
            [],
            new RunOptions { ThreadId = "happy-1", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        terminal.State!["child_out"].ShouldBe("ok");

        var childDone = await sharedCheckpointer.GetAsync("happy-1/nested");
        childDone!.Status.ShouldBe(GraphRunStatus.Done);
    }

    private static CompiledGraph BuildChildWithGate(InMemoryCheckpointer checkpointer)
    {
        return new StateGraph()
            .AddChannel("child_out", ChannelKind.LastValue)
            .AddNode(
                "gate",
                static (context, _) => context.ResumePayload is null
                    ? Task.FromResult<NodeResult>(
                        NodeResult.Interrupt(new { need = "child-signoff" }))
                    : Task.FromResult<NodeResult>(
                        NodeResult.Continue(new ChannelWrite("child_out", "child-done"))))
            .AddEdge(GraphConstants.Start, "gate")
            .AddEdge("gate", GraphConstants.End)
            .Compile(checkpointer);
    }
}
