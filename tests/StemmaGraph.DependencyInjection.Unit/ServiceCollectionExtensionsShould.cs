using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StemmaGraph.Abstractions.Results;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph;
using StemmaGraph.Graph.Builder;
using Xunit;

namespace StemmaGraph.DependencyInjection.Unit;

public sealed class ServiceCollectionExtensionsShould
{
    [Fact(DisplayName = "Given compiled graph, when AddStemmaGraph is called, then graph resolves as singleton")]
    public void RegisterCompiledGraphAsSingleton()
    {
        var graph = CreateLinearGraph();

        var services = new ServiceCollection();
        services.AddStemmaGraph(graph);

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<CompiledGraph>();
        var second = provider.GetRequiredService<CompiledGraph>();

        first.ShouldBeSameAs(graph);
        second.ShouldBeSameAs(first);
    }

    [Fact(DisplayName = "Given factory, when AddStemmaGraph is called, then factory graph is singleton")]
    public void RegisterFactoryGraphAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddStemmaGraph(static _ => CreateLinearGraph());

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<CompiledGraph>();
        var second = provider.GetRequiredService<CompiledGraph>();

        first.ShouldBeSameAs(second);
    }

    private static CompiledGraph CreateLinearGraph()
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
