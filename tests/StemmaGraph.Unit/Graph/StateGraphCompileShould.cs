using Shouldly;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Abstractions.Results;
using StemmaGraph.Checkpoint;
using StemmaGraph.Exceptions;
using StemmaGraph.Graph.Builder;
using Xunit;

namespace StemmaGraph.Unit.Graph;

public sealed class StateGraphCompileShould
{
    [Fact(DisplayName = "Given missing START edge, when Compile is called, then fails")]
    public void FailWhenStartMissing()
    {
        var builder = new StateGraph()
            .AddNode("a", static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue()));

        var exception = Should.Throw<GraphCompileException>(
            () => builder.Compile(new InMemoryCheckpointer()));

        exception.Code.ShouldBe("graph.missing_start");
    }

    [Fact(DisplayName = "Given duplicate node name, when AddNode is called, then fails")]
    public void FailOnDuplicateNode()
    {
        var builder = new StateGraph()
            .AddNode("a", static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue()));

        var exception = Should.Throw<GraphCompileException>(
            () => builder.AddNode("a", static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue())));

        exception.Code.ShouldBe("graph.duplicate_node");
    }

    [Fact(DisplayName = "Given edge to unknown node, when Compile is called, then fails")]
    public void FailOnUnknownEndpoint()
    {
        var builder = new StateGraph()
            .AddNode("a", static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue()))
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", "missing");

        var exception = Should.Throw<GraphCompileException>(
            () => builder.Compile(new InMemoryCheckpointer()));

        exception.Code.ShouldBe("graph.unknown_endpoint");
    }

    [Fact(DisplayName = "Given a cycle, when Compile is called, then succeeds")]
    public void AllowCycles()
    {
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode("agent", static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue()))
            .AddNode("tools", static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue()))
            .AddEdge(GraphConstants.Start, "agent")
            .AddEdge("agent", "tools")
            .AddEdge("tools", "agent")
            .Compile(new InMemoryCheckpointer());

        graph.ShouldNotBeNull();
    }
}
