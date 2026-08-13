// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using Shouldly;
using StemmaGraph;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph;
using StemmaGraph.Abstractions.Results;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Exceptions;
using StemmaGraph.Abstractions.Streaming;
using Xunit;
using StemmaGraph.Graph.Builder;

namespace StemmaGraph.Unit.Runtime;

public sealed class ParallelSuperstepShould
{
    [Fact(DisplayName = "Given two START edges, when run, then both nodes execute same superstep and Append merges")]
    public async Task FanOutAppendMerge()
    {
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode(
                "left",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "L"))))
            .AddNode(
                "right",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("messages", "R"))))
            .AddEdge(GraphConstants.Start, "left")
            .AddEdge(GraphConstants.Start, "right")
            .AddEdge("left", GraphConstants.End)
            .AddEdge("right", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "fan-1", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        var messages = terminal.State!["messages"].ShouldBeOfType<List<object?>>();
        messages.ShouldBe(["L", "R"]);
    }

    [Fact(DisplayName = "Given two LastValue writers same step, when run, then fails concurrent update")]
    public async Task LastValueTwoWritersFails()
    {
        var graph = new StateGraph()
            .AddChannel("status", ChannelKind.LastValue)
            .AddNode(
                "left",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("status", "L"))))
            .AddNode(
                "right",
                static (_, _) => Task.FromResult<NodeResult>(
                    NodeResult.Continue(new ChannelWrite("status", "R"))))
            .AddEdge(GraphConstants.Start, "left")
            .AddEdge(GraphConstants.Start, "right")
            .AddEdge("left", GraphConstants.End)
            .AddEdge("right", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        await Should.ThrowAsync<GraphConcurrentUpdateException>(async () =>
        {
            await graph.InvokeAsync([], new RunOptions { ThreadId = "lv-1" });
        });
    }

    [Fact(DisplayName = "Given two parallel nodes, when both read shared channel, then neither sees peer write from same step")]
    public async Task BarrierHidesPeerWrites()
    {
        var leftSaw = "unset";
        var rightSaw = "unset";

        var barrierGraph = new StateGraph()
            .AddChannel("status", ChannelKind.LastValue)
            .AddNode(
                "left",
                (context, _) =>
                {
                    leftSaw = context.Read<string>("status");
                    return Task.FromResult<NodeResult>(
                        NodeResult.Continue(new ChannelWrite("status", "from-left")));
                })
            .AddNode(
                "right",
                (context, _) =>
                {
                    rightSaw = context.Read<string>("status");
                    return Task.FromResult<NodeResult>(NodeResult.Continue());
                })
            .AddEdge(GraphConstants.Start, "left")
            .AddEdge(GraphConstants.Start, "right")
            .AddEdge("left", GraphConstants.End)
            .AddEdge("right", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var terminal = await barrierGraph.InvokeAsync(
            [new ChannelWrite("status", "seed")],
            new RunOptions { ThreadId = "barrier-2", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        leftSaw.ShouldBe("seed");
        rightSaw.ShouldBe("seed");
        terminal.State!["status"].ShouldBe("from-left");
    }
}
