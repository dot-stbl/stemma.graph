using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Checkpoint;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit.Graph;

public sealed class DescribeTopologyShould
{
    [Fact(DisplayName = "Given compiled graph, when Describe, then nodes edges channels are exported")]
    public void ExportNodesEdgesChannels()
    {
        var graph = new StateGraph()
            .AddChannel("messages", ChannelKind.Append)
            .AddNode("a", static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue()))
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());

        var description = graph.Describe();

        description.Nodes.ShouldBe(["a"]);
        description.Channels["messages"].ShouldBe(ChannelKind.Append);
        description.StaticEdges.Count.ShouldBe(2);
        description.ConditionalSources.ShouldBeEmpty();
    }
}
