using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Checkpoint;
using Voluta.Graph;
using Voluta.Graph.Builder;
using Voluta.Graph.Options;
using Voluta.Tools.Tools;
using Xunit;

namespace Voluta.Tools.Unit;

public sealed class ToolGraphNodeShould
{
    [Fact(DisplayName = "Given DelegateTool, when ToolGraphNode is invoked, then writes result text to output channel")]
    public async Task WriteToolResultToChannel()
    {
        var tool = DelegateTool.Create(
            "echo",
            (call, _) =>
            {
                var message = call.Arguments.TryGetValue("message", out var value)
                    ? value?.ToString() ?? ""
                    : "";
                return Task.FromResult(ToolResult.Ok($"echo:{message}"));
            },
            description: "echoes message");

        var graph = new StateGraph()
            .AddChannel("tool_out", ChannelKind.LastValue)
            .AddNode(
                "tool",
                ToolGraphNode.Create(
                    tool,
                    "tool_out",
                    new Dictionary<string, object?> { ["message"] = "hi" },
                    timeout: null))
            .AddEdge(GraphConstants.Start, "tool")
            .AddEdge("tool", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "t-tool-1", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        terminal.State.ShouldNotBeNull();
        terminal.State!["tool_out"].ShouldBe("echo:hi");
    }

    [Fact(DisplayName = "Given soft error without ThrowOnError, when tool fails, then writes text and error channel")]
    public async Task WriteSoftErrorWithoutThrowing()
    {
        var tool = DelegateTool.Create(
            "fail",
            static (_, _) => Task.FromResult(ToolResult.Error("boom")));

        var node = new ToolGraphNode(
            tool,
            new ToolNodeOptions
            {
                OutputChannel = "tool_out",
                ErrorChannel = "tool_err",
            });

        var graph = new StateGraph()
            .AddChannel("tool_out", ChannelKind.LastValue)
            .AddChannel("tool_err", ChannelKind.LastValue)
            .AddNode("tool", node)
            .AddEdge(GraphConstants.Start, "tool")
            .AddEdge("tool", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "t-tool-2", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        terminal.State!["tool_out"].ShouldBe("boom");
        terminal.State!["tool_err"].ShouldBe(true);
    }

    [Fact(DisplayName = "Given timeout, when tool hangs, then throws ToolInvocationException")]
    public async Task ThrowWhenToolTimesOut()
    {
        var tool = DelegateTool.Create(
            "slow",
            static async (_, cancellationToken) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                return ToolResult.Ok("late");
            });

        var node = ToolGraphNode.Create(
            tool,
            "tool_out",
            arguments: null,
            timeout: TimeSpan.FromMilliseconds(50));

        var context = new GraphContext(
            "tool",
            new Dictionary<string, object?>(StringComparer.Ordinal));

        var exception = await Should.ThrowAsync<ToolInvocationException>(
            async () => await node.InvokeAsync(context, CancellationToken.None));

        exception.ToolName.ShouldBe("slow");
        exception.Message.ShouldContain("timed out");
    }

    [Fact(DisplayName = "Given callFactory, when invoked, then builds ToolCall from GraphContext")]
    public async Task BuildCallFromContextFactory()
    {
        var tool = DelegateTool.Create(
            "add",
            (call, _) =>
            {
                var left = Convert.ToInt32(call.Arguments["a"]);
                var right = Convert.ToInt32(call.Arguments["b"]);
                return Task.FromResult(ToolResult.Ok((left + right).ToString()));
            });

        var graph = new StateGraph()
            .AddChannel("a", ChannelKind.LastValue)
            .AddChannel("b", ChannelKind.LastValue)
            .AddChannel("sum", ChannelKind.LastValue)
            .AddNode(
                "add",
                ToolGraphNode.Create(
                    tool,
                    "sum",
                    context => new ToolCall(
                        "add",
                        new Dictionary<string, object?>
                        {
                            ["a"] = context.Read<int>("a"),
                            ["b"] = context.Read<int>("b"),
                        })))
            .AddEdge(GraphConstants.Start, "add")
            .AddEdge("add", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var terminal = await graph.InvokeAsync(
            [new ChannelWrite("a", 2), new ChannelWrite("b", 3)],
            new RunOptions { ThreadId = "t-tool-3", StreamMode = StreamMode.Values });

        terminal.State!["sum"].ShouldBe("5");
    }
}
