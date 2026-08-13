using StemmaGraph.Abstractions.Results;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph;
using StemmaGraph.Graph.Builder;

namespace StemmaGraph.DependencyInjection.Unit;

/// <summary>
///     Minimal linear graph for DI registration tests.
/// </summary>
internal static class LinearGraphFixture
{
    public static CompiledGraph Create()
    {
        return new StateGraph()
            .AddNode(
                "a",
                static async (_, _) =>
                {
                    await Task.CompletedTask;
                    return NodeResult.Continue();
                })
            .AddEdge(GraphConstants.Start, "a")
            .AddEdge("a", GraphConstants.End)
            .Compile(new InMemoryCheckpointer());
    }
}
