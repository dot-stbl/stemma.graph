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
using StemmaGraph.Graph.Options;
using StemmaGraph.Exceptions.Run;

namespace StemmaGraph.Unit.Runtime;

public sealed class ConditionalAndCycleShould
{
    [Fact(DisplayName = "Given conditional edge, when status is tools, then routes to tools then END")]
    public async Task BranchOnStatus()
    {
        var visited = new List<string>();

        var graph = new StateGraph()
            .AddChannel("status", ChannelKind.LastValue)
            .AddNode(
                "agent",
                (context, _) =>
                {
                    visited.Add("agent");
                    return Task.FromResult<NodeResult>(NodeResult.Continue());
                })
            .AddNode(
                "tools",
                (context, _) =>
                {
                    visited.Add("tools");
                    return Task.FromResult<NodeResult>(
                        NodeResult.Continue(new ChannelWrite("status", "done")));
                })
            .AddEdge(GraphConstants.Start, "agent")
            .AddConditionalEdges(
                "agent",
                context => context.Read<string>("status") == "tools" ? "tools" : GraphConstants.End)
            .AddEdge("tools", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var terminal = await graph.InvokeAsync(
            [new ChannelWrite("status", "tools")],
            new RunOptions { ThreadId = "cond-1", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        visited.ShouldBe(["agent", "tools"]);
    }

    [Fact(DisplayName = "Given infinite cycle, when recursion limit exceeded, then fails out of steps")]
    public async Task CycleHitsRecursionLimit()
    {
        var graph = new StateGraph()
            .AddChannel("n", ChannelKind.LastValue)
            .AddNode(
                "loop",
                static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue()))
            .AddEdge(GraphConstants.Start, "loop")
            .AddEdge("loop", "loop")
            .Compile(new InMemoryCheckpointer(), new CompileOptions { RecursionLimit = 3 });

        var exception = await Should.ThrowAsync<GraphOutOfStepsException>(async () =>
        {
            await graph.InvokeAsync([], new RunOptions { ThreadId = "cycle-1" });
        });

        exception.Limit.ShouldBe(3);
        exception.Code.ShouldBe("graph.out_of_steps");
    }
}
