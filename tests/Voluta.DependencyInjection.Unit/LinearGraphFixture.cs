using Voluta.Abstractions.Results;
using Voluta.Checkpoint;
using Voluta.Graph;
using Voluta.Graph.Builder;

namespace Voluta.DependencyInjection.Unit;

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
