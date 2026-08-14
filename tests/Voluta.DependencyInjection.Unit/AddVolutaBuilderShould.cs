using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Results;
using Voluta.Checkpoint;
using Voluta.DependencyInjection;
using Voluta.DependencyInjection.Checkpoints;
using Voluta.Graph;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.DependencyInjection.Unit;

public sealed class AddVolutaBuilderShould
{
    [Fact(DisplayName = "Given Checkpoints.UseInMemory and Graph factory, when resolved, then both are singletons")]
    public void RegisterCheckpointsAndGraphTogether()
    {
        var services = new ServiceCollection();
        services.AddVoluta(voluta =>
        {
            voluta.Checkpoints.UseInMemory();
            voluta.Graph((_, checkpointer) => new StateGraph()
                .AddNode(
                    "a",
                    static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue()))
                .AddEdge(GraphConstants.Start, "a")
                .AddEdge("a", GraphConstants.End)
                .Compile(checkpointer));
        });

        using var provider = services.BuildServiceProvider();
        var checkpointer = provider.GetRequiredService<ICheckpointer>();
        var graph = provider.GetRequiredService<CompiledGraph>();
        var graphAgain = provider.GetRequiredService<CompiledGraph>();

        checkpointer.ShouldBeOfType<InMemoryCheckpointer>();
        graph.ShouldBeSameAs(graphAgain);
    }

    [Fact(DisplayName = "Given Graph with checkpointer param but no Use*, when AddVoluta runs, then throws")]
    public void ThrowWhenGraphNeedsCheckpointsButNoneConfigured()
    {
        var services = new ServiceCollection();

        var exception = Should.Throw<InvalidOperationException>(() =>
            services.AddVoluta(voluta =>
                voluta.Graph((_, checkpointer) => LinearGraphFixture.Create())));

        exception.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Given Graph factory that ignores checkpointer without Use*, when AddVoluta runs, then still throws")]
    public void ThrowWhenCheckpointerParameterPresentWithoutUse()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddVoluta(voluta =>
                voluta.Graph((_, _) => new StateGraph()
                    .AddNode(
                        "a",
                        static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue()))
                    .AddEdge(GraphConstants.Start, "a")
                    .AddEdge("a", GraphConstants.End)
                    .Compile(new InMemoryCheckpointer()))));
    }

    [Fact(DisplayName = "Given only Checkpoints.UseInMemory, when resolved, then ICheckpointer is registered without graph")]
    public void RegisterCheckpointsOnly()
    {
        var services = new ServiceCollection();
        services.AddVoluta(voluta => voluta.Checkpoints.UseInMemory());

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICheckpointer>().ShouldBeOfType<InMemoryCheckpointer>();
        provider.GetService<CompiledGraph>().ShouldBeNull();
    }
}
